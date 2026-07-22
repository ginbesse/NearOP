namespace NearOP
{
    partial class Form1
    {
        /// <summary>
        ///Gerekli tasarımcı değişkeni.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstDevices;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.PictureBox picScreen;
        private System.Windows.Forms.GroupBox grpControls;
        private System.Windows.Forms.Button btnSendCommand;
        private System.Windows.Forms.TextBox txtCommand;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtStreamUrl;
        private System.Windows.Forms.Button btnOpenUrl;

        /// <summary>
        ///Kullanılan tüm kaynakları temizleyin.
        /// </summary>
        ///<param name="disposing">yönetilen kaynaklar dispose edilmeliyse doğru; aksi halde yanlış.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer üretilen kod

        /// <summary>
        /// Tasarımcı desteği için gerekli metot - bu metodun 
        ///içeriğini kod düzenleyici ile değiştirmeyin.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Text = "NearOP - Cihaz Kontrolü";

            // Devices list
            this.lstDevices = new System.Windows.Forms.ListBox();
            this.lstDevices.Location = new System.Drawing.Point(12, 12);
            this.lstDevices.Size = new System.Drawing.Size(300, 450);
            this.lstDevices.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstDevices.DoubleClick += new System.EventHandler(this.lstDevices_DoubleClick);
            this.Controls.Add(this.lstDevices);

            // Scan button
            this.btnScan = new System.Windows.Forms.Button();
            this.btnScan.Location = new System.Drawing.Point(12, 480);
            this.btnScan.Size = new System.Drawing.Size(140, 40);
            this.btnScan.Text = "Tarama Başlat";
            this.btnScan.Click += new System.EventHandler(this.btnScan_Click);
            this.Controls.Add(this.btnScan);

            // Connect button
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnConnect.Location = new System.Drawing.Point(172, 480);
            this.btnConnect.Size = new System.Drawing.Size(140, 40);
            this.btnConnect.Text = "Bağlan";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            this.Controls.Add(this.btnConnect);

            // PictureBox for remote screen
            this.picScreen = new System.Windows.Forms.PictureBox();
            this.picScreen.Location = new System.Drawing.Point(330, 12);
            this.picScreen.Size = new System.Drawing.Size(640, 360);
            this.picScreen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picScreen.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picScreen.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Controls.Add(this.picScreen);

            // Control buttons area
            this.grpControls = new System.Windows.Forms.GroupBox();
            this.grpControls.Location = new System.Drawing.Point(330, 390);
            this.grpControls.Size = new System.Drawing.Size(640, 140);
            this.grpControls.Text = "Cihaz Kontrolleri";
            this.grpControls.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));

            this.btnSendCommand = new System.Windows.Forms.Button();
            this.btnSendCommand.Location = new System.Drawing.Point(520, 30);
            this.btnSendCommand.Size = new System.Drawing.Size(100, 30);
            this.btnSendCommand.Text = "Gönder";
            this.btnSendCommand.Click += new System.EventHandler(this.btnSendCommand_Click);

            this.txtCommand = new System.Windows.Forms.TextBox();
            this.txtCommand.Location = new System.Drawing.Point(20, 30);
            this.txtCommand.Size = new System.Drawing.Size(480, 30);

            this.lblStatus = new System.Windows.Forms.Label();
            this.lblStatus.Location = new System.Drawing.Point(20, 70);
            this.lblStatus.Size = new System.Drawing.Size(600, 50);
            this.lblStatus.Text = "Durum: Bağlı değil";

            this.grpControls.Controls.Add(this.txtCommand);
            this.grpControls.Controls.Add(this.btnSendCommand);
            this.grpControls.Controls.Add(this.lblStatus);
            this.Controls.Add(this.grpControls);

            // Stream URL textbox and open button
            this.txtStreamUrl = new System.Windows.Forms.TextBox();
            this.txtStreamUrl.Location = new System.Drawing.Point(330, 540);
            this.txtStreamUrl.Size = new System.Drawing.Size(520, 25);
            this.txtStreamUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtStreamUrl.Text = "";
            this.Controls.Add(this.txtStreamUrl);

            this.btnOpenUrl = new System.Windows.Forms.Button();
            this.btnOpenUrl.Location = new System.Drawing.Point(860, 536);
            this.btnOpenUrl.Size = new System.Drawing.Size(110, 30);
            this.btnOpenUrl.Text = "Aç";
            this.btnOpenUrl.Click += new System.EventHandler(this.btnOpenUrl_Click);
            this.btnOpenUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Controls.Add(this.btnOpenUrl);
        }

        #endregion
    }
}

