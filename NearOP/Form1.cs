using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Windows.Forms;

namespace NearOP
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource _scanCts;
        private System.Windows.Forms.Timer _frameTimer;
        private Random _rnd = new Random();
        private bool _connected = false;
        private MjpegStreamer _streamer;
        private CancellationTokenSource _streamCts;

        public Form1()
        {
            InitializeComponent();

            // Prepare frame timer (kept for fallback simulated frames)
            _frameTimer = new System.Windows.Forms.Timer();
            _frameTimer.Interval = 100; // 10 fps simulated
            _frameTimer.Tick += FrameTimer_Tick;
        }

        private void lstDevices_DoubleClick(object sender, EventArgs e)
        {
            // On double click, attempt connect to selected device
            try
            {
                btnConnect_Click(this, EventArgs.Empty);
            }
            catch { }
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            if (_scanCts != null)
            {
                // Stop scanning
                _scanCts.Cancel();
                _scanCts = null;
                btnScan.Text = "Tarama Başlat";
                return;
            }

            lstDevices.Items.Clear();
            btnScan.Text = "Durdur";
            _scanCts = new CancellationTokenSource();
            var token = _scanCts.Token;

            try
            {
                await Task.Run(async () =>
                {
                    // 1) SSDP discovery
                    try
                    {
                        var ssdpResults = await DiscoverSsdpAsync(3000, token);
                        foreach (var url in ssdpResults)
                        {
                            if (token.IsCancellationRequested) break;
                            try
                            {
                                var uri = new Uri(url);
                                var name = uri.Host;
                                var dev = new DeviceInfo { Name = "UPnP:" + name, Address = uri.Host };
                                this.Invoke(new Action(() => lstDevices.Items.Add(dev)));
                            }
                            catch { }
                        }
                    }
                    catch { }

                    // 2) quick HTTP port scan on local /24
                    try
                    {
                        var localIp = GetLocalIPv4();
                        if (!string.IsNullOrEmpty(localIp))
                        {
                            var baseIp = string.Join(".", localIp.Split('.').Take(3));
                            var sem = new SemaphoreSlim(40);
                            var tasks = new List<Task>();
                            for (int i = 1; i < 255; i++)
                            {
                                if (token.IsCancellationRequested) break;
                                var ip = baseIp + "." + i;
                                if (ip == localIp) continue;
                                await sem.WaitAsync(token);
                                tasks.Add(Task.Run(async () =>
                                {
                                    try
                                    {
                                        // update UI about current probe
                                        this.Invoke(new Action(() => lblStatus.Text = $"Tarama: {ip}"));
                                        int[] ports = new[] { 80, 8080 };
                                        foreach (var port in ports)
                                        {
                                            if (token.IsCancellationRequested) break;
                                            using (var tcp = new TcpClient())
                                            {
                                                try
                                                {
                                                    var connectTask = tcp.ConnectAsync(ip, port);
                                                    var done = await Task.WhenAny(connectTask, Task.Delay(400, token));
                                                    if (done == connectTask && tcp.Connected)
                                                    {
                                                        // probe for MJPEG on this port
                                                        var streamUrl = await ProbeCommonMjpegPathsAsync(ip, port, token);
                                                        if (!string.IsNullOrEmpty(streamUrl))
                                                        {
                                                            var dev = new DeviceInfo { Name = "HTTP:" + ip, Address = ip, StreamUrl = streamUrl };
                                                            this.Invoke(new Action(() => lstDevices.Items.Add(dev)));
                                                            break; // found on this host
                                                        }
                                                        else
                                                        {
                                                            var dev = new DeviceInfo { Name = "HTTP:" + ip, Address = ip };
                                                            this.Invoke(new Action(() => lstDevices.Items.Add(dev)));
                                                            break;
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                    catch { }
                                    finally { sem.Release(); }
                                }, token));
                            }
                            try { await Task.WhenAll(tasks); } catch { }
                        }
                    }
                    catch { }
                }, token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _scanCts = null;
                btnScan.Text = "Tarama Başlat";
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!_connected)
            {
                if (lstDevices.SelectedItem is DeviceInfo di)
                {
                    _connected = true;
                    lblStatus.Text = $"Durum: {di.Name} ({di.Address}) - Bağlanılıyor...";
                    btnConnect.Text = "Bağlantıyı Kes";

                    // If we already know a stream URL, start it; otherwise try to discover one.
                    _streamCts = new CancellationTokenSource();
                    var token = _streamCts.Token;
                    Task.Run(async () =>
                    {
                        try
                        {
                            var url = di.StreamUrl;
                            if (string.IsNullOrEmpty(url))
                            {
                                url = await ProbeCommonMjpegPathsAsync(di.Address, 80, token);
                                if (!string.IsNullOrEmpty(url)) di.StreamUrl = url;
                            }

                            if (!string.IsNullOrEmpty(url))
                            {
                                this.Invoke(new Action(() => lblStatus.Text = $"Durum: Akış açıılıyor -> {url}"));
                                StartStream(url);
                            }
                            else
                            {
                                this.Invoke(new Action(() =>
                                {
                                    lblStatus.Text = "Durum: Akış bulunamadı. Manuel URL girin.";
                                }));
                            }
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            this.Invoke(new Action(() => lblStatus.Text = "Durum: Akış hatası: " + ex.Message));
                        }
                    }, token);
                }
                else
                {
                    MessageBox.Show("Bağlanmak için bir cihaz seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                // Disconnect
                _connected = false;
                lblStatus.Text = "Durum: Bağlı değil";
                btnConnect.Text = "Bağlan";
                _streamCts?.Cancel();
                StopStream();
                picScreen.Image = null;
            }
        }

        private void FrameTimer_Tick(object sender, EventArgs e)
        {
            // Simulated remote screen frame. Replace this with decoded frames from the remote device.
            var bmp = GenerateTestFrame(picScreen.Width, picScreen.Height);
            var old = picScreen.Image;
            picScreen.Image = bmp;
            old?.Dispose();
        }

        private void btnOpenUrl_Click(object sender, EventArgs e)
        {
            var url = txtStreamUrl.Text?.Trim();
            if (string.IsNullOrEmpty(url)) return;

            if (_streamer != null)
            {
                // stop
                StopStream();
                lblStatus.Text = "Durum: Akış durduruldu";
                btnOpenUrl.Text = "Aç";
            }
            else
            {
                StartStream(url);
                lblStatus.Text = "Durum: Akış açıldı";
                btnOpenUrl.Text = "Durdur";
            }
        }

        private Bitmap GenerateTestFrame(int w, int h)
        {
            if (w <= 0) w = 640;
            if (h <= 0) h = 360;
            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                // moving rectangle
                int x = _rnd.Next(0, Math.Max(1, w - 100));
                int y = _rnd.Next(0, Math.Max(1, h - 60));
                var rect = new Rectangle(x, y, 100, 60);
                g.FillRectangle(Brushes.OrangeRed, rect);
                g.DrawString(DateTime.Now.ToString("HH:mm:ss.fff"), SystemFonts.DefaultFont, Brushes.White, 10, 10);
            }
            return bmp;
        }

        private void StartStream(string url)
        {
            try
            {
                StopStream();
                _streamer = new MjpegStreamer(picScreen);
                _streamer.Start(url);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Durum: Stream başlatılamadı - " + ex.Message;
            }
        }

        private void StopStream()
        {
            try
            {
                _streamer?.Stop();
            }
            catch { }
            finally { _streamer = null; }
        }

        private void btnSendCommand_Click(object sender, EventArgs e)
        {
            if (!_connected)
            {
                MessageBox.Show("Önce cihaza bağlanın.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cmd = txtCommand.Text?.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            // TODO: Gerçek komut gönderme mantığını burada uygulayın (soket, HTTP, BLE characteristic write vb.)
            lblStatus.Text = $"Durum: Komut gönderildi -> {cmd} ({DateTime.Now:HH:mm:ss})";
            txtCommand.Clear();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            try
            {
                _scanCts?.Cancel();
            }
            catch { }
            _frameTimer?.Stop();
            picScreen?.Image?.Dispose();
            _streamCts?.Cancel();
            StopStream();
        }

        private class DeviceInfo
        {
            public string Name { get; set; }
            public string Address { get; set; }
            public string StreamUrl { get; set; }
            public override string ToString() => string.IsNullOrEmpty(StreamUrl) ? $"{Name} ({Address})" : $"{Name} ({Address}) - {StreamUrl}";
        }

        // --- network helpers ---
        private string GetLocalIPv4()
        {
            try
            {
                var hosts = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                foreach (var ip in hosts)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
                }
            }
            catch { }
            return null;
        }

        private async Task<List<string>> DiscoverSsdpAsync(int timeoutMs, CancellationToken token)
        {
            var results = new List<string>();
            using (var udp = new UdpClient())
            {
                udp.Client.ReceiveTimeout = timeoutMs;
                var req = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nMX: 2\r\nST: ssdp:all\r\n\r\n";
                var data = Encoding.ASCII.GetBytes(req);
                var ep = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
                await udp.SendAsync(data, data.Length, ep);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < timeoutMs && !token.IsCancellationRequested)
                {
                    try
                    {
                        var r = await udp.ReceiveAsync();
                        var resp = Encoding.ASCII.GetString(r.Buffer);
                        var lines = resp.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase))
                            {
                                var loc = line.Substring(9).Trim();
                                results.Add(loc);
                            }
                        }
                    }
                    catch { break; }
                }
            }
            return results.Distinct().ToList();
        }

        private async Task<string> ProbeCommonMjpegPathsAsync(string host, int port, CancellationToken token)
        {
            var paths = new[] { "/video", "/mjpeg", "/video.cgi", "/stream", "/cam.mjpg", "/camera.mjpg", "/axis-cgi/mjpg/video.cgi" };
            foreach (var p in paths)
            {
                if (token.IsCancellationRequested) break;
                var url = $"http://{host}:{port}{p}";
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "GET";
                    req.Timeout = 1500;
                    req.ReadWriteTimeout = 1500;
                    using (var resp = (HttpWebResponse)await req.GetResponseAsync())
                    {
                        var ct = resp.ContentType ?? "";
                        if (ct.IndexOf("multipart", StringComparison.OrdinalIgnoreCase) >= 0 || ct.IndexOf("jpeg", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return url;
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        private class MjpegStreamer
        {
            private PictureBox _target;
            private Thread _thread;
            private volatile bool _stop;
            public MjpegStreamer(PictureBox target)
            {
                _target = target;
            }
            public void Start(string url)
            {
                Stop();
                _stop = false;
                _thread = new Thread(() => Run(url)) { IsBackground = true };
                _thread.Start();
            }
            public void Stop()
            {
                _stop = true;
                try { _thread?.Join(500); } catch { }
                _thread = null;
            }
            private void Run(string url)
            {
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Timeout = 5000;
                    req.AllowWriteStreamBuffering = false;
                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var stream = resp.GetResponseStream())
                    {
                        var ms = new MemoryStream();
                        var buffer = new byte[4096];
                        int read;
                        while (!_stop && (read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                            // try to find JPEG frame boundaries
                            var data = ms.GetBuffer();
                            int len = (int)ms.Length;
                            int start = -1, end = -1;
                            for (int i = 0; i < len - 1; i++)
                            {
                                if (data[i] == 0xFF && data[i + 1] == 0xD8) { start = i; break; }
                            }
                            for (int j = start + 2; j < len - 1; j++)
                            {
                                if (data[j] == 0xFF && data[j + 1] == 0xD9) { end = j + 2; break; }
                            }
                            if (start >= 0 && end > start)
                            {
                                try
                                {
                                    var frame = new byte[end - start];
                                    Array.Copy(data, start, frame, 0, frame.Length);
                                    using (var fms = new MemoryStream(frame))
                                    using (var bmp = Image.FromStream(fms))
                                    {
                                        var old = _target.Image;
                                        var bmpClone = new Bitmap(bmp);
                                        _target.Invoke(new Action(() => _target.Image = bmpClone));
                                        old?.Dispose();
                                    }
                                }
                                catch { }
                                // remove consumed bytes
                                var remaining = len - end;
                                var tmp = new byte[remaining];
                                Array.Copy(data, end, tmp, 0, remaining);
                                ms.SetLength(0);
                                ms.Write(tmp, 0, remaining);
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }
}
