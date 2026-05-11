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
using Windows.Media.SpeechSynthesis;
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
    private System.Media.SoundPlayer _player = new();

    private FormWindowState _previousWindowState;
    private FormBorderStyle _previousBorderStyle;
    private Control _previousParent;

    private readonly Color BackColorDark = Color.FromArgb(30, 30, 46);
    private readonly Color SurfaceColor = Color.FromArgb(49, 50, 68);
    private readonly Color TextColor = Color.FromArgb(205, 214, 244);
    private readonly Color AccentColor = Color.FromArgb(137, 180, 250);
    private readonly Color SecondaryAccentColor = Color.FromArgb(203, 166, 247);

    public Form1(ILLMService llmService, IImageGenerationService imageService, ITtsService ttsService, IVideoService videoService, IDatabaseService dbService)
    {
        InitializeComponent();
        _llmService = llmService;
        _imageService = imageService;
        _ttsService = ttsService;
        _videoService = videoService;
        _dbService = dbService;
        ApplyTheme();
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

        StyleButton(btnGenerateStory, SecondaryAccentColor, Color.FromArgb(17, 17, 27));
        StyleButton(btnGenerateVideo, SecondaryAccentColor, Color.FromArgb(17, 17, 27));
        StyleButton(btnSpeak, SurfaceColor, TextColor);
        StyleButton(btnStop, SurfaceColor, TextColor);
        StyleButton(btnToggleLog, SurfaceColor, TextColor);

        btnGenerateStory.Text = "Hikaye Olu\u015Ftur";
        btnGenerateVideo.Text = "Video Olu\u015Ftur";
        btnToggleLog.Text = "\u25BC Loglar";

        tabControlMedia.BackColor = BackColorDark;
        tabPageImages.BackColor = BackColorDark;
        tabPageVideo.BackColor = BackColorDark;
        flowLayoutPanelImages.BackColor = BackColorDark;
    }

    private void StyleTextBox(TextBox tb)
    {
        tb.BackColor = SurfaceColor;
        tb.ForeColor = TextColor;
        tb.BorderStyle = BorderStyle.FixedSingle;
        tb.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
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
            _currentStory.Content = await _llmService.GenerateStoryAsync(_currentStory.Prompt);

            if (string.IsNullOrEmpty(_currentStory.Content))
                throw new Exception("LLM boş içerik döndürdü.");

            Log("Hikaye başlığı oluşturuluyor...");
            _currentStory.Title = await _llmService.GenerateTitleAsync(_currentStory.Content);

            txtStoryContent.Text = $"{(_currentStory.Title ?? "BÖLÜM").ToUpper()}\r\n\r\n{_currentStory.Content}";

            Log("DALL-E ile görseller üretiliyor...");
            _currentStory.ImagePaths = await _imageService.GenerateImagesAsync(_currentStory.Content, 3);       

            flowLayoutPanelImages.Controls.Clear();
            foreach (var path in _currentStory.ImagePaths)
            {
                if (File.Exists(path))
                {
                    var pb = new PictureBox
                    {
                        Image = Image.FromFile(path),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Width = 330,
                        Height = 330,
                        Margin = new Padding(5)
                    };
                    flowLayoutPanelImages.Controls.Add(pb);
                }
            }

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

            string html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ margin: 0; background: #000; display: flex; justify-content: center; align-items: center; height: 100vh; overflow: hidden; font-family: 'Segoe UI', sans-serif; color: white; }}
                        .video-container {{ position: relative; width: 100%; height: 100%; display: flex; justify-content: center; align-items: center; overflow: hidden; }}
                        video {{ max-width: 100%; max-height: 100%; transition: filter 2s ease-in-out; }}
                        
                        /* Efekt Tanımları */
                        .effect-noir {{ filter: grayscale(100%) contrast(1.2) brightness(0.9); }}
                        .effect-vintage {{ filter: sepia(0.6) contrast(1.1) brightness(0.9) saturate(0.7); }}
                        .effect-dreamy {{ filter: brightness(1.1) saturate(1.3) blur(0.3px); }}
                        .effect-warm {{ filter: sepia(0.2) saturate(1.6) hue-rotate(-10deg); }}
                        .effect-cool {{ filter: hue-rotate(180deg) saturate(1.2) brightness(1.1); }}
                        
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
                        
                        .effect-label {{
                            position: absolute; top: 20px; right: 20px;
                            background: rgba(0,0,0,0.5); padding: 5px 12px; border-radius: 15px;
                            font-size: 12px; letter-spacing: 1px; color: #89b4fa;
                            opacity: 0; transition: opacity 0.5s; z-index: 20;
                            text-transform: uppercase; border: 1px solid rgba(137,180,250,0.3);
                        }}
                    </style>
                </head>
                <body>
                    <div class='video-container'>
                        <div id='playButton' onclick='startVideo()'></div>
                        <video id='myVideo' controls style='display:none;'>
                            <source src='{videoUrl}' type='video/mp4'>
                        </video>
                        <div id='vignette' class='vignette'></div>
                        <div id='effectLabel' class='effect-label'>Sinematik Mod</div>
                    </div>

                    <script>
                        var v = document.getElementById('myVideo');
                        var vignette = document.getElementById('vignette');
                        var label = document.getElementById('effectLabel');
                        
                        const effects = ['none', 'noir', 'vintage', 'dreamy', 'warm', 'cool'];
                        let currentEffectIndex = 0;
                        let effectInterval;

                        function startVideo() {{
                            v.style.display = 'block';
                            document.getElementById('playButton').style.display = 'none';
                            v.play();
                            
                            // Efekt geçişlerini başlat
                            vignette.style.opacity = '1';
                            label.style.opacity = '0.7';
                            
                            effectInterval = setInterval(cycleEffect, 6000); // Her 6 saniyede bir değiştir
                        }}

                        function cycleEffect() {{
                            if (v.paused || v.ended) return;

                            currentEffectIndex = (currentEffectIndex + 1) % effects.length;
                            const effect = effects[currentEffectIndex];
                            
                            v.className = '';
                            if (effect !== 'none') {{
                                v.classList.add('effect-' + effect);
                                label.innerText = 'Efekt: ' + effect;
                            }} else {{
                                label.innerText = 'Efekt: Normal';
                            }}
                            
                            // Etiketi kısa süreliğine parlat
                            label.style.opacity = '1';
                            setTimeout(() => {{ label.style.opacity = '0.6'; }}, 1000);
                        }}

                        v.onended = function() {{
                            clearInterval(effectInterval);
                            label.style.opacity = '0';
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
            _player.SoundLocation = audioPath;
            _player.Play();
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
            _player.Stop();
            Log("Seslendirme durduruldu.");
        }
        catch (Exception ex)
        {
            Log("Durdurma hatası: " + ex.Message);
        }
    }

        private void btnToggleLog_Click(object sender, EventArgs e)
    {
        txtLog.Visible = !txtLog.Visible;
        btnToggleLog.Text = txtLog.Visible ? "\u25BC Loglar" : "\u25B6 Loglar";

        if (txtLog.Visible)
        {
            txtStoryContent.Height = 200;
            btnToggleLog.Top = 255;
            btnSpeak.Top = 225;
            btnStop.Top = 225;
        }
        else
        {
            btnToggleLog.Top = 406;
            txtStoryContent.Height = 351;
            btnSpeak.Top = 376;
            btnStop.Top = 376;
        }
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
}
