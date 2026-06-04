using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using hikaye_olusturucu.Core.Interfaces;
using hikaye_olusturucu.Core.Models;
using Microsoft.Web.WebView2.Core;
using NAudio.Wave;
using System.Drawing;
using System.Windows.Forms;

namespace hikaye_olusturucu;

public partial class Form1 : Form
{
    private readonly ILLMService _llmService;
    private readonly IImageGenerationService _imageService;
    private readonly ITtsService _ttsService;
    private readonly IVideoService _videoService;
    private readonly IDatabaseService _dbService;

    private Story _currentStory = new();
    
    private WaveOutEvent _waveOut;
    private AudioFileReader _audioFileReader;

    private Button btnSaveText;
    private Button btnSaveAudio;
    private Button btnSaveVideo;

    private FormWindowState _previousWindowState;
    private FormBorderStyle _previousBorderStyle;
    private Control _previousParent;

    private readonly Color BackColorDark = Color.FromArgb(30, 30, 46);
    private readonly Color SurfaceColor = Color.FromArgb(49, 50, 68);
    private readonly Color TextColor = Color.FromArgb(205, 214, 244);
    private readonly Color AccentColor = Color.FromArgb(137, 180, 250);
    private readonly Color SecondaryAccentColor = Color.FromArgb(203, 166, 247);

    [System.Runtime.InteropServices.DllImport("uxtheme.dll", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

    public Form1(ILLMService llmService, IImageGenerationService imageService, ITtsService ttsService, IVideoService videoService, IDatabaseService dbService)
    {
        InitializeComponent();
        _llmService = llmService;
        _imageService = imageService;
        _ttsService = ttsService;
        _videoService = videoService;
        _dbService = dbService;
        
        txtPrompt.KeyDown += TxtPrompt_KeyDown;
        
        ApplyTheme();
    }

    private void TxtPrompt_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            if (!e.Shift)
            {
                e.SuppressKeyPress = true;
                if (btnGenerateStory.Enabled)
                {
                    btnGenerateStory.PerformClick();
                }
            }
        }
    }

    private void ApplyTheme()
    {
        this.BackColor = BackColorDark;
        this.ForeColor = TextColor;
        this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

        lblPrompt.ForeColor = TextColor;

        StyleTextBox(txtPrompt);
        StyleTextBox(txtStoryContent);
        StyleTextBox(txtLog);

        btnSpeak.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnStop.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        btnSaveText = new Button();
        btnSaveText.Text = "⭳";
        btnSaveText.Size = new Size(25, 25);
        btnSaveText.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        StyleButton(btnSaveText, SurfaceColor, TextColor);
        btnSaveText.Click += BtnSaveText_Click;
        this.Controls.Add(btnSaveText);

        btnSaveAudio = new Button();
        btnSaveAudio.Text = "⭳";
        btnSaveAudio.Size = new Size(25, 25);
        btnSaveAudio.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        StyleButton(btnSaveAudio, SurfaceColor, TextColor);
        btnSaveAudio.Click += BtnSaveAudio_Click;
        this.Controls.Add(btnSaveAudio);

        btnGenerateVideo.Width = 115;
        
        btnSaveVideo = new Button();
        btnSaveVideo.Text = "⭳";
        btnSaveVideo.Size = new Size(25, 30);
        StyleButton(btnSaveVideo, SurfaceColor, TextColor);
        btnSaveVideo.Click += BtnSaveVideo_Click;
        btnSaveVideo.Location = new Point(btnGenerateVideo.Right + 5, btnGenerateVideo.Top);
        btnSaveVideo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.Controls.Add(btnSaveVideo);

        StyleButton(btnGenerateStory, SecondaryAccentColor, Color.FromArgb(17, 17, 27));
        StyleButton(btnGenerateVideo, SecondaryAccentColor, Color.FromArgb(17, 17, 27));
        StyleButton(btnSpeak, SurfaceColor, TextColor);
        StyleButton(btnStop, SurfaceColor, TextColor);
        StyleButton(btnToggleLog, SurfaceColor, TextColor);

        btnGenerateStory.Text = "Hikaye Oluştur";
        btnGenerateVideo.Text = "Video Oluştur";
        txtLog.Visible = false;
        btnToggleLog.Text = "▶ Loglar";

        tabControlMedia.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabControlMedia.SizeMode = TabSizeMode.Fixed;
        tabControlMedia.DrawItem += TabControlMedia_DrawItem;
        tabControlMedia.BackColor = BackColorDark;
        tabPageImages.BackColor = BackColorDark;
        tabPageVideo.BackColor = BackColorDark;
        tabPageImages.BorderStyle = BorderStyle.None;
        tabPageVideo.BorderStyle = BorderStyle.None;
        flowLayoutPanelImages.BorderStyle = BorderStyle.None;
        flowLayoutPanelImages.BackColor = BackColorDark;
        
        this.Resize += (s, e) => UpdateLeftPanelLayout();
        UpdateLeftPanelLayout();
    }

    private void TabControlMedia_DrawItem(object sender, DrawItemEventArgs e)
    {
        var tc = (TabControl)sender;
        var tp = tc.TabPages[e.Index];
        var r = tc.GetTabRect(e.Index);

        // 1. Sekmenin kendi arka planını boya
        using var backBrush = new SolidBrush(BackColorDark);
        e.Graphics.FillRectangle(backBrush, r);

        // Sekme metnini yazdır
        var text = tp.Text;
        var font = new Font(tc.Font, FontStyle.Bold);
        var textSize = e.Graphics.MeasureString(text, font);

        var textX = r.Left + (r.Width - textSize.Width) / 2;
        var textY = r.Top + (r.Height - textSize.Height) / 2;

        using var textBrush = new SolidBrush(SecondaryAccentColor);
        e.Graphics.DrawString(text, font, textBrush, textX, textY);

        // ==========================================
        // YENİ KISIM: Beyaz Çerçeveyi Griye Dönüştürme
        // ==========================================
        if (e.Index == tc.TabCount - 1)
        {
            // Sol taraftaki TextBox'ların gri kenarlık rengine (SurfaceColor) eşitleyebilirsin.
            // Eğer bu gri az gelirse Color.FromArgb(100, 100, 110) gibi statik bir gri de verebilirsin.
            using var borderPen = new Pen(Color.Gray, 2);

            // TabControl'ün iç sayfa sınırlarını (beyaz çizgilerin olduğu yeri) yakala
            Rectangle displayRect = tc.DisplayRectangle;

            // Beyaz çizgiyi tamamen kapatması için alanı 1 piksel genişletiyoruz
            displayRect.X -= 1;
            displayRect.Y -= 1;
            displayRect.Width += 2;
            displayRect.Height += 2;

            // Beyaz parlamanın üzerine gri çerçevemizi çekiyoruz
            e.Graphics.DrawRectangle(borderPen, displayRect);
        }
    }

    private void StyleTextBox(TextBox tb)
    {
        tb.BackColor = SurfaceColor;
        tb.ForeColor = TextColor;
        tb.BorderStyle = BorderStyle.FixedSingle;
        tb.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

        try
        {
            SetWindowTheme(tb.Handle, "DarkMode_Explorer", null);
        }
        catch { }
    }

    private void StyleButton(Button btn, Color backColor, Color foreColor)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = backColor;
        btn.ForeColor = foreColor;
        btn.Cursor = Cursors.Hand;
        btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
    }

    private async void Form1_Load(object sender, EventArgs e)
    {
        Log("Sistem başlatılıyor...");
        try
        {
            await _dbService.InitializeDatabaseAsync();
            Log("Veritabanı hazır.");

            var environmentOptions = new CoreWebView2EnvironmentOptions() { Language = "tr-TR" };
            var environment = await CoreWebView2Environment.CreateAsync(null, null, environmentOptions);        
            await webViewVideo.EnsureCoreWebView2Async(environment);

            webViewVideo.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", AppDomain.CurrentDomain.BaseDirectory, CoreWebView2HostResourceAccessKind.Allow);
            webViewVideo.CoreWebView2.ContainsFullScreenElementChanged += CoreWebView2_ContainsFullScreenElementChanged;

            Log("Dahili Video Oynatıcı (WebView2) hazır.");
        }
        catch (Exception ex)
        {
            Log("Başlatma hatası: " + ex.Message);
        }
    }

    private void CoreWebView2_ContainsFullScreenElementChanged(object sender, object e)
    {
        if (webViewVideo.CoreWebView2.ContainsFullScreenElement)
        {
            _previousWindowState = this.WindowState;
            _previousBorderStyle = this.FormBorderStyle;
            _previousParent = webViewVideo.Parent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            webViewVideo.Parent = this;
            webViewVideo.BringToFront();
        }
        else
        {
            webViewVideo.Parent = _previousParent;
            this.FormBorderStyle = _previousBorderStyle;
            this.WindowState = _previousWindowState;
        }
    }

    private async void btnGenerateStory_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPrompt.Text))
        {
            MessageBox.Show("Lütfen bir prompt girin.");
            return;
        }

        try
        {
            ToggleUI(false);
            _currentStory = new Story { Prompt = txtPrompt.Text.Trim() };

            Log("Hikaye LLM ile oluşturuluyor...");
            string generatedContent = await _llmService.GenerateStoryAsync(_currentStory.Prompt);

            if (generatedContent.StartsWith("[YEDEK API KULLANILDI]"))
            {
                generatedContent = generatedContent.Replace("[YEDEK API KULLANILDI]", "").Trim();
                MessageBox.Show("Ana servis (Pollinations) şu anda yoğun olduğu için hikaye alternatif bir ücretsiz servis (Yedek API) kullanılarak oluşturuldu.", "Servis Yoğunluğu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log("Ana servis yoğun. Yedek API kullanıldı.");
            }

            _currentStory.Content = generatedContent;

            if (string.IsNullOrEmpty(_currentStory.Content))
                throw new Exception("LLM boş içerik döndürdü.");

            Log("Hikaye başlığı oluşturuluyor...");
            _currentStory.Title = await _llmService.GenerateTitleAsync(_currentStory.Content);

            txtStoryContent.Text = $"{(_currentStory.Title ?? "BÖLÜM").ToUpper()}\r\n\r\n{_currentStory.Content}";

            Log("Görseller yapay zeka ile üretiliyor...");
            _currentStory.ImagePaths = await _imageService.GenerateImagesAsync(_currentStory.Content, 3);       

            if (_currentStory.ImagePaths.Any(p => Path.GetFileName(p).StartsWith("fallback_image")))
            {
                MessageBox.Show("Görsel servisi şu anda yoğun olduğu için hikayeye yer tutucu (fallback) görseller eklendi. Video oluşturma işlemine sorunsuz devam edebilirsiniz.", "Servis Yoğunluğu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log("Görsel servisi yoğun. Yer tutucu görseller kullanıldı.");
            }

            // 1. Eski kontrol ve görselleri temizleyerek dosya kilitlerini ve belleği serbest bırakalım
            flowLayoutPanelImages.SuspendLayout();
            foreach (Control ctrl in flowLayoutPanelImages.Controls)
            {
                if (ctrl is Panel panelCtrl)
                {
                    foreach (Control innerCtrl in panelCtrl.Controls)
                    {
                        if (innerCtrl is PictureBox pbCtrl)
                        {
                            if (pbCtrl.Image != null)
                            {
                                var img = pbCtrl.Image;
                                pbCtrl.Image = null;
                                img.Dispose();
                            }
                        }
                    }
                }
                ctrl.Dispose();
            }
            flowLayoutPanelImages.Controls.Clear();

            // 2. Yeni görselleri yükleyelim
            foreach (var path in _currentStory.ImagePaths)
            {
                if (File.Exists(path))
                {
                    var panel = new Panel
                    {
                        Width = 330,
                        Height = 330,
                        Margin = new Padding(5)
                    };

                    // Dosya kilidi oluşturmamak için görseli hafızaya kopyalayarak yükleyelim
                    Image imgCopy = null;
                    try
                    {
                        using (var tempImg = Image.FromFile(path))
                        {
                            imgCopy = new Bitmap(tempImg);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Görsel yükleme hatası ({Path.GetFileName(path)}): {ex.Message}");
                    }

                    var pb = new PictureBox
                    {
                        Image = imgCopy,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Dock = DockStyle.Fill
                    };

                    var btnDownloadImg = new Button
                    {
                        Text = "📥",
                        Size = new Size(35, 35),
                        BackColor = SurfaceColor,
                        ForeColor = TextColor,
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand,
                        Font = new Font("Segoe UI", 12F)
                    };
                    btnDownloadImg.FlatAppearance.BorderSize = 0;
                    btnDownloadImg.Tag = path;
                    btnDownloadImg.Click += (s, ev) =>
                    {
                        using (SaveFileDialog sfd = new SaveFileDialog())
                        {
                            sfd.Filter = "Görsel|*.png;*.jpg;*.jpeg";
                            sfd.Title = "Görseli Farklı Kaydet";
                            sfd.FileName = Path.GetFileName((string)((Button)s).Tag);
                            if (sfd.ShowDialog() == DialogResult.OK)
                            {
                                File.Copy((string)((Button)s).Tag, sfd.FileName, true);
                                MessageBox.Show("Görsel başarıyla kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    };

                    panel.Controls.Add(btnDownloadImg);
                    panel.Controls.Add(pb);

                    btnDownloadImg.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                    btnDownloadImg.Location = new Point(panel.Width - btnDownloadImg.Width - 5, panel.Height - btnDownloadImg.Height - 5);
                    btnDownloadImg.BringToFront();

                    flowLayoutPanelImages.Controls.Add(panel);
                }
            }
            flowLayoutPanelImages.ResumeLayout(true);

            tabControlMedia.SelectedTab = tabPageImages;

            Log("TTS ile hikaye seslendiriliyor...");
            _currentStory.AudioPath = await _ttsService.GenerateAudioAsync(_currentStory.Content);

            Log("Kayıt veritabanına ekleniyor...");
            await _dbService.SaveStoryAsync(_currentStory);

            Log("1. Aşama tamamlandı! Artık video oluşturabilirsiniz.");
            btnGenerateVideo.Enabled = true;
        }
        catch (Exception ex)
        {
            Log($"HATA: {ex.Message}");
            if (ex.InnerException != null) Log($"Detay: {ex.InnerException.Message}");
            MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleUI(true);
        }
    }

    private async void btnGenerateVideo_Click(object sender, EventArgs e)
    {
        try
        {
            ToggleUI(false);
            Log("FFmpeg ile video işleniyor...");

            _currentStory.VideoPath = await _videoService.CreateVideoAsync(
                _currentStory.ImagePaths,
                _currentStory.AudioPath,
                _currentStory.Content,
                _currentStory.Title);

            Log("Video oluşturuldu: " + _currentStory.VideoPath);
            await _dbService.SaveStoryAsync(_currentStory);

            tabControlMedia.SelectedTab = tabPageVideo;

            string fileName = Path.GetFileName(_currentStory.VideoPath);
            string videoUrl = $"https://app.local/{fileName}";
            
            // WebVTT dosyasını Base64 formatına çevirerek MIME tipi veya CORS sorunlarını aşalım
            string vttFileName = Path.ChangeExtension(fileName, ".vtt");
            string vttPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, vttFileName);
            string vttContent = File.Exists(vttPath) ? File.ReadAllText(vttPath) : "WEBVTT\n\n";
            string vttBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(vttContent));
            string vttDataUrl = $"data:text/vtt;charset=utf-8;base64,{vttBase64}";

            string html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ margin: 0; background: #000; display: flex; justify-content: center; align-items: center; height: 100vh; overflow: hidden; font-family: 'Segoe UI', sans-serif; color: white; }}
                        .video-container {{ position: relative; width: 100vw; max-width: 100vh; aspect-ratio: 1/1; display: flex; justify-content: center; align-items: center; overflow: hidden; }}
                        video {{ width: 100%; height: 100%; object-fit: contain; }}       

                        .vignette {{
                            position: absolute; top: 0; left: 0; width: 100%; height: 100%;
                            pointer-events: none;
                            background: radial-gradient(circle, transparent 30%, rgba(0,0,0,0.6) 100%);
                            z-index: 5; opacity: 0; transition: opacity 2s ease;
                        }}

                        #playButton {{
                            position: absolute;
                            width: 100px; height: 100px;
                            background: rgba(137, 180, 250, 0.4);
                            border-radius: 50%;
                            border: 3px solid #89b4fa;
                            display: flex; justify-content: center; align-items: center;
                            cursor: pointer; transition: 0.3s; z-index: 10;
                        }}
                        #playButton:hover {{ transform: scale(1.1); background: rgba(137, 180, 250, 0.6); }}    
                        #playButton::after {{
                            content: ''; border-style: solid; border-width: 20px 0 20px 32px;
                            border-color: transparent transparent transparent white; margin-left: 8px;
                        }}
                        
                        /* Subtitle styling */
                        video::cue {{
                            background-color: transparent;
                            color: white;
                            text-shadow: 2px 2px 4px #000000, -2px -2px 4px #000000, 2px -2px 4px #000000, -2px 2px 4px #000000;
                            font-size: 1.5em;
                            font-family: 'Segoe UI', sans-serif;
                        }}
                    </style>
                </head>
                <body>
                    <div class='video-container'>
                        <div id='playButton' onclick='startVideo()'></div>
                        <video id='myVideo' controls style='display:none;' crossorigin='anonymous'>
                            <source src='{videoUrl}' type='video/mp4'>
                            <track id='subtitleTrack' src='{vttDataUrl}' kind='subtitles' srclang='tr' label='Türkçe' default>
                        </video>
                        <div id='vignette' class='vignette'></div>
                    </div>

                    <script>
                        var v = document.getElementById('myVideo');
                        var vignette = document.getElementById('vignette');

                        function startVideo() {{
                            v.style.display = 'block';
                            document.getElementById('playButton').style.display = 'none';
                            v.play();
                            vignette.style.opacity = '1';
                            
                            // Track nesnesini bul ve 'showing' olarak zorla
                            setTimeout(() => {{
                                if (v.textTracks && v.textTracks.length > 0) {{
                                    v.textTracks[0].mode = 'showing';
                                }}
                            }}, 100);
                        }}

                        v.onended = function() {{
                            vignette.style.opacity = '0';
                        }};
                    </script>
                </body>
                </html>";

            webViewVideo.CoreWebView2.NavigateToString(html);
        }
        catch (Exception ex)
        {
            Log($"Video Hatası: {ex.Message}");
            MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleUI(true);
        }
    }

    private async void btnSpeak_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtStoryContent.Text)) return;
        try
        {
            btnSpeak.Enabled = false;
            Log("Metin seslendiriliyor...");
            string audioPath = await _ttsService.GenerateAudioAsync(txtStoryContent.Text);
            
            StopAudio(); // Eski sesi temizle

            _audioFileReader = new AudioFileReader(audioPath);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_audioFileReader);
            _waveOut.Play();
            
            Log("Seslendirme başladı.");
        }
        catch (Exception ex)
        {
            Log("Seslendirme hatası: " + ex.Message);
        }
        finally
        {
            btnSpeak.Enabled = true;
        }
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        try
        {
            StopAudio();
            Log("Seslendirme durduruldu.");
        }
        catch (Exception ex)
        {
            Log("Durdurma hatası: " + ex.Message);
        }
    }

    private void StopAudio()
    {
        if (_waveOut != null)
        {
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }
        if (_audioFileReader != null)
        {
            _audioFileReader.Dispose();
            _audioFileReader = null;
        }
    }

    private void UpdateLeftPanelLayout()
    {
        int rowHeight = 35; 

        if (txtLog.Visible)
        {
            txtLog.Height = 151;
            txtLog.Top = this.ClientSize.Height - txtLog.Height - 4;
            btnToggleLog.Top = txtLog.Top - btnToggleLog.Height - 4;
        }
        else
        {
            btnToggleLog.Top = this.ClientSize.Height - btnToggleLog.Height - 4;
        }

        txtStoryContent.Height = btnToggleLog.Top - txtStoryContent.Top - rowHeight - 4;

        int buttonY = txtStoryContent.Bottom + 5;

        btnStop.Top = buttonY;
        btnStop.Left = txtStoryContent.Right - btnStop.Width;

        btnSpeak.Top = buttonY;
        btnSpeak.Left = btnStop.Left - btnSpeak.Width - 5;

        if (btnSaveAudio != null)
        {
            btnSaveAudio.Top = buttonY;
            btnSaveAudio.Left = btnSpeak.Left - btnSaveAudio.Width - 15;
        }

        if (btnSaveText != null)
        {
            btnSaveText.Top = buttonY;
            btnSaveText.Left = txtStoryContent.Left;
        }
    }

    private void btnToggleLog_Click(object sender, EventArgs e)
    {
        if (sender != null) txtLog.Visible = !txtLog.Visible;
        btnToggleLog.Text = txtLog.Visible ? "▼ Loglar" : "▶ Loglar";
        UpdateLeftPanelLayout();
    }

    private void Log(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
    }

    private void ToggleUI(bool enabled)
    {
        btnGenerateStory.Enabled = enabled;
        txtPrompt.Enabled = enabled;
    }

    private void BtnSaveText_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtStoryContent.Text))
        {
            MessageBox.Show("Kaydedilecek metin bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using (SaveFileDialog sfd = new SaveFileDialog())
        {
            sfd.Filter = "Metin Dosyası|*.txt";
            sfd.Title = "Metni Farklı Kaydet";
            sfd.FileName = string.IsNullOrWhiteSpace(_currentStory.Title) ? "Hikaye.txt" : $"{_currentStory.Title}.txt";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, txtStoryContent.Text);
                MessageBox.Show("Metin başarıyla kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void BtnSaveAudio_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentStory.AudioPath) || !File.Exists(_currentStory.AudioPath))
        {
            MessageBox.Show("Kaydedilecek ses dosyası bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using (SaveFileDialog sfd = new SaveFileDialog())
        {
            sfd.Filter = "WAV Ses Dosyası|*.wav|MP3 Ses Dosyası|*.mp3";
            sfd.Title = "Ses Dosyasını Farklı Kaydet";
            sfd.FileName = string.IsNullOrWhiteSpace(_currentStory.Title) ? "Ses.wav" : $"{_currentStory.Title}.wav";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.Copy(_currentStory.AudioPath, sfd.FileName, true);
                MessageBox.Show("Ses başarıyla kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void BtnSaveVideo_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentStory.VideoPath) || !File.Exists(_currentStory.VideoPath))
        {
            MessageBox.Show("Kaydedilecek video bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using (SaveFileDialog sfd = new SaveFileDialog())
        {
            sfd.Filter = "MP4 Video|*.mp4";
            sfd.Title = "Videoyu Farklı Kaydet";
            sfd.FileName = string.IsNullOrWhiteSpace(_currentStory.Title) ? "Video.mp4" : $"{_currentStory.Title}.mp4";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.Copy(_currentStory.VideoPath, sfd.FileName, true);
                MessageBox.Show("Video başarıyla kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}

public class BorderlessTabControl : TabControl
{
    private const int TCM_ADJUSTRECT = 0x1328;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == TCM_ADJUSTRECT && !DesignMode)
        {
            m.Result = (IntPtr)1;
            return;
        }
        base.WndProc(ref m);
    }
}
