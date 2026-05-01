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
            
            this.lblPrompt.AutoSize = true;
            this.lblPrompt.Location = new System.Drawing.Point(12, 15);
            this.lblPrompt.Name = "lblPrompt";
            this.lblPrompt.Size = new System.Drawing.Size(46, 15);
            this.lblPrompt.TabIndex = 6;
            this.lblPrompt.Text = "Prompt:";
            
            this.txtPrompt.Location = new System.Drawing.Point(64, 12);
            this.txtPrompt.Name = "txtPrompt";
            this.txtPrompt.Size = new System.Drawing.Size(460, 23);
            this.txtPrompt.TabIndex = 0;
            
            this.btnGenerateStory.Location = new System.Drawing.Point(530, 11);
            this.btnGenerateStory.Name = "btnGenerateStory";
            this.btnGenerateStory.Size = new System.Drawing.Size(120, 25);
            this.btnGenerateStory.TabIndex = 1;
            this.btnGenerateStory.Text = "Hikaye Oluştur";
            this.btnGenerateStory.UseVisualStyleBackColor = true;
            this.btnGenerateStory.Click += new System.EventHandler(this.btnGenerateStory_Click);
            
            this.btnGenerateVideo.Enabled = false;
            this.btnGenerateVideo.Location = new System.Drawing.Point(656, 11);
            this.btnGenerateVideo.Name = "btnGenerateVideo";
            this.btnGenerateVideo.Size = new System.Drawing.Size(120, 25);
            this.btnGenerateVideo.TabIndex = 4;
            this.btnGenerateVideo.Text = "Video Oluştur";
            this.btnGenerateVideo.UseVisualStyleBackColor = true;
            this.btnGenerateVideo.Click += new System.EventHandler(this.btnGenerateVideo_Click);
            
            this.txtStoryContent.Location = new System.Drawing.Point(15, 45);
            this.txtStoryContent.Multiline = true;
            this.txtStoryContent.Name = "txtStoryContent";
            this.txtStoryContent.ReadOnly = true;
            this.txtStoryContent.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStoryContent.Size = new System.Drawing.Size(380, 200);
            this.txtStoryContent.TabIndex = 2;
            
            this.tabControlMedia.Controls.Add(this.tabPageImages);
            this.tabControlMedia.Controls.Add(this.tabPageVideo);
            this.tabControlMedia.Location = new System.Drawing.Point(410, 45);
            this.tabControlMedia.Name = "tabControlMedia";
            this.tabControlMedia.SelectedIndex = 0;
            this.tabControlMedia.Size = new System.Drawing.Size(366, 366);
            this.tabControlMedia.TabIndex = 3;
            
            this.tabPageImages.Controls.Add(this.flowLayoutPanelImages);
            this.tabPageImages.Location = new System.Drawing.Point(4, 24);
            this.tabPageImages.Name = "tabPageImages";
            this.tabPageImages.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageImages.Size = new System.Drawing.Size(358, 338);
            this.tabPageImages.TabIndex = 0;
            this.tabPageImages.Text = "Görseller";
            this.tabPageImages.UseVisualStyleBackColor = true;
            
            this.flowLayoutPanelImages.AutoScroll = true;
            this.flowLayoutPanelImages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelImages.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanelImages.Name = "flowLayoutPanelImages";
            this.flowLayoutPanelImages.Size = new System.Drawing.Size(352, 332);
            this.flowLayoutPanelImages.TabIndex = 0;
            
            this.tabPageVideo.Controls.Add(this.webViewVideo);
            this.tabPageVideo.Location = new System.Drawing.Point(4, 24);
            this.tabPageVideo.Name = "tabPageVideo";
            this.tabPageVideo.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageVideo.Size = new System.Drawing.Size(358, 338);
            this.tabPageVideo.TabIndex = 1;
            this.tabPageVideo.Text = "Video Oynatıcı";
            this.tabPageVideo.UseVisualStyleBackColor = true;
            
            this.webViewVideo.AllowExternalDrop = true;
            this.webViewVideo.CreationProperties = null;
            this.webViewVideo.DefaultBackgroundColor = System.Drawing.Color.Black;
            this.webViewVideo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webViewVideo.Location = new System.Drawing.Point(3, 3);
            this.webViewVideo.Name = "webViewVideo";
            this.webViewVideo.Size = new System.Drawing.Size(352, 332);
            this.webViewVideo.TabIndex = 0;
            this.webViewVideo.ZoomFactor = 1D;
            
            this.txtLog.Location = new System.Drawing.Point(15, 260);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(380, 151);
            this.txtLog.TabIndex = 5;
            
            this.ClientSize = new System.Drawing.Size(790, 430);
            this.Controls.Add(this.lblPrompt);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnGenerateVideo);
            this.Controls.Add(this.tabControlMedia);
            this.Controls.Add(this.txtStoryContent);
            this.Controls.Add(this.btnGenerateStory);
            this.Controls.Add(this.txtPrompt);
            this.Name = "Form1";
            this.Text = "AI Hikaye ve Video Oluşturucu";
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