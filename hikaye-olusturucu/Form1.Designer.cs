namespace hikaye_olusturucu
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtPrompt;
        private System.Windows.Forms.Button btnGenerateStory;
        private System.Windows.Forms.TextBox txtStoryContent;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelImages;
        private System.Windows.Forms.Button btnGenerateVideo;
        private System.Windows.Forms.Button btnSpeak;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnToggleLog;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblPrompt;
        private System.Windows.Forms.TabControl tabControlMedia;
        private System.Windows.Forms.TabPage tabPageImages;
        private System.Windows.Forms.TabPage tabPageVideo;
        private Microsoft.Web.WebView2.WinForms.WebView2 webViewVideo;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtPrompt = new System.Windows.Forms.TextBox();
            this.btnGenerateStory = new System.Windows.Forms.Button();
            this.txtStoryContent = new System.Windows.Forms.TextBox();
            this.flowLayoutPanelImages = new System.Windows.Forms.FlowLayoutPanel();
            this.btnGenerateVideo = new System.Windows.Forms.Button();
            this.btnSpeak = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnToggleLog = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.lblPrompt = new System.Windows.Forms.Label();
            this.tabControlMedia = new System.Windows.Forms.TabControl();
            this.tabPageImages = new System.Windows.Forms.TabPage();
            this.tabPageVideo = new System.Windows.Forms.TabPage();
            this.webViewVideo = new Microsoft.Web.WebView2.WinForms.WebView2();

            this.tabControlMedia.SuspendLayout();
            this.tabPageImages.SuspendLayout();
            this.tabPageVideo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webViewVideo)).BeginInit();
            this.SuspendLayout();

            // 
            // lblPrompt
            // 
            this.lblPrompt.AutoSize = true;
            this.lblPrompt.Location = new System.Drawing.Point(12, 18);
            this.lblPrompt.Name = "lblPrompt";
            this.lblPrompt.Size = new System.Drawing.Size(55, 15);
            this.lblPrompt.TabIndex = 6;
            this.lblPrompt.Text = "Prompt:";

            // 
            // txtPrompt
            // 
            this.txtPrompt.Location = new System.Drawing.Point(85, 10); // Aradaki boÅŸluk artÄ±rÄ±ldÄ± (X=85)
            this.txtPrompt.Multiline = true;
            this.txtPrompt.Name = "txtPrompt";
            this.txtPrompt.Size = new System.Drawing.Size(415, 30); // GeniÅŸlik Ã§akÄ±ÅŸmayÄ± Ã¶nleyecek ÅŸekilde ayarlandÄ±
            this.txtPrompt.TabIndex = 0;

            // 
            // btnGenerateStory
            // 
            this.btnGenerateStory.Location = new System.Drawing.Point(510, 10);
            this.btnGenerateStory.Name = "btnGenerateStory";
            this.btnGenerateStory.Size = new System.Drawing.Size(145, 30);
            this.btnGenerateStory.TabIndex = 1;
            this.btnGenerateStory.Text = "Hikaye Olu\u015Ftur";
            this.btnGenerateStory.UseVisualStyleBackColor = true;
            this.btnGenerateStory.Click += new System.EventHandler(this.btnGenerateStory_Click);

            // 
            // btnGenerateVideo
            // 
            this.btnGenerateVideo.Enabled = false;
            this.btnGenerateVideo.Location = new System.Drawing.Point(660, 10);
            this.btnGenerateVideo.Name = "btnGenerateVideo";
            this.btnGenerateVideo.Size = new System.Drawing.Size(145, 30);
            this.btnGenerateVideo.TabIndex = 4;
            this.btnGenerateVideo.Text = "Video Olu\u015Ftur";
            this.btnGenerateVideo.UseVisualStyleBackColor = true;
            this.btnGenerateVideo.Click += new System.EventHandler(this.btnGenerateVideo_Click);

            // 
            // txtStoryContent
            // 
            this.txtStoryContent.Location = new System.Drawing.Point(15, 55);
            this.txtStoryContent.Multiline = true;
            this.txtStoryContent.Name = "txtStoryContent";
            this.txtStoryContent.ReadOnly = true;
            this.txtStoryContent.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStoryContent.Size = new System.Drawing.Size(380, 200);
            this.txtStoryContent.TabIndex = 2;

            // 
            // btnSpeak
            // 
            this.btnSpeak.BackColor = System.Drawing.SystemColors.ControlLight;
            this.btnSpeak.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSpeak.Location = new System.Drawing.Point(315, 225);
            this.btnSpeak.Name = "btnSpeak";
            this.btnSpeak.Size = new System.Drawing.Size(25, 25);
            this.btnSpeak.TabIndex = 7;
            this.btnSpeak.Text = "\uD83D\uDD0A";
            this.btnSpeak.UseVisualStyleBackColor = false;
            this.btnSpeak.Click += new System.EventHandler(this.btnSpeak_Click);

            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.SystemColors.ControlLight;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Location = new System.Drawing.Point(342, 225);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(25, 25);
            this.btnStop.TabIndex = 8;
            this.btnStop.Text = "\u25A0";
            this.btnStop.UseVisualStyleBackColor = false;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);

            // 
            // tabControlMedia
            // 
            this.tabControlMedia.Controls.Add(this.tabPageImages);
            this.tabControlMedia.Controls.Add(this.tabPageVideo);
            this.tabControlMedia.Location = new System.Drawing.Point(410, 55);
            this.tabControlMedia.Name = "tabControlMedia";
            this.tabControlMedia.SelectedIndex = 0;
            this.tabControlMedia.Size = new System.Drawing.Size(390, 366);
            this.tabControlMedia.TabIndex = 3;
            this.tabControlMedia.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControlMedia.ItemSize = new System.Drawing.Size(193, 32);

            // 
            // tabPageImages
            // 
            this.tabPageImages.Controls.Add(this.flowLayoutPanelImages);
            this.tabPageImages.Location = new System.Drawing.Point(4, 36);
            this.tabPageImages.Name = "tabPageImages";
            this.tabPageImages.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageImages.Size = new System.Drawing.Size(382, 326);
            this.tabPageImages.TabIndex = 0;
            this.tabPageImages.Text = "G\u00F6rseller";
            this.tabPageImages.UseVisualStyleBackColor = true;

            // 
            // flowLayoutPanelImages
            // 
            this.flowLayoutPanelImages.AutoScroll = true;
            this.flowLayoutPanelImages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelImages.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanelImages.Name = "flowLayoutPanelImages";
            this.flowLayoutPanelImages.Size = new System.Drawing.Size(376, 320);
            this.flowLayoutPanelImages.TabIndex = 0;

            // 
            // tabPageVideo
            // 
            this.tabPageVideo.Controls.Add(this.webViewVideo);
            this.tabPageVideo.Location = new System.Drawing.Point(4, 36);
            this.tabPageVideo.Name = "tabPageVideo";
            this.tabPageVideo.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageVideo.Size = new System.Drawing.Size(382, 326);
            this.tabPageVideo.TabIndex = 1;
            this.tabPageVideo.Text = "Video Oynat\u0131c\u0131";
            this.tabPageVideo.UseVisualStyleBackColor = true;

            // 
            // webViewVideo
            // 
            this.webViewVideo.AllowExternalDrop = true;
            this.webViewVideo.CreationProperties = null;
            this.webViewVideo.DefaultBackgroundColor = System.Drawing.Color.Black;
            this.webViewVideo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webViewVideo.Location = new System.Drawing.Point(3, 3);
            this.webViewVideo.Name = "webViewVideo";
            this.webViewVideo.Size = new System.Drawing.Size(376, 320);
            this.webViewVideo.TabIndex = 0;
            this.webViewVideo.ZoomFactor = 1D;

            // 
            // btnToggleLog
            // 
            this.btnToggleLog.Location = new System.Drawing.Point(15, 255);
            this.btnToggleLog.Name = "btnToggleLog";
            this.btnToggleLog.Size = new System.Drawing.Size(380, 25);
            this.btnToggleLog.TabIndex = 9;
            this.btnToggleLog.Text = "\u25BC Loglar";
            this.btnToggleLog.UseVisualStyleBackColor = true;
            this.btnToggleLog.Click += new System.EventHandler(this.btnToggleLog_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(15, 280);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(380, 151);
            this.txtLog.TabIndex = 5;

            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(815, 435);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnSpeak);
            this.Controls.Add(this.lblPrompt);
            this.Controls.Add(this.btnToggleLog);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnGenerateVideo);
            this.Controls.Add(this.tabControlMedia);
            this.Controls.Add(this.txtStoryContent);
            this.Controls.Add(this.btnGenerateStory);
            this.Controls.Add(this.txtPrompt);
            this.Name = "Form1";
            this.Text = "AI Hikaye ve Video Olu\u015Fturucu";
            this.Load += new System.EventHandler(this.Form1_Load);

            this.tabControlMedia.ResumeLayout(false);
            this.tabPageImages.ResumeLayout(false);
            this.tabPageVideo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.webViewVideo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
