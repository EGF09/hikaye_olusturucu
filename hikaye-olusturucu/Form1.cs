using System;
using System.Drawing;
using System.Windows.Forms;
using hikaye_olusturucu.Core.Interfaces;
using hikaye_olusturucu.Core.Models;
using Microsoft.Web.WebView2.Core;

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

    // Tam ekran durumu için önceki ayarları saklayacağımız değişkenler
    private FormWindowState _previousWindowState;
    private FormBorderStyle _previousBorderStyle;
    private Control _previousParent;

    public Form1(ILLMService llmService, IImageGenerationService imageService, ITtsService ttsService, IVideoService videoService, IDatabaseService dbService)
    {
        InitializeComponent();
        _llmService = llmService;
        _imageService = imageService;
        _ttsService = ttsService;
        _videoService = videoService;
        _dbService = dbService;
    }

    private async void Form1_Load(object sender, EventArgs e)
    {
        Log("Sistem başlatılıyor...");
        try 
        {
            await _dbService.InitializeDatabaseAsync();
            Log("Veritabanı hazır.");
            
            // WebView2 Başlatılıyor: Oynatıcı kontrollerini (Play, Pause vb.) Türkçe yapmak için dil ayarı veriyoruz
            var environmentOptions = new CoreWebView2EnvironmentOptions() { Language = "tr-TR" };
            var environment = await CoreWebView2Environment.CreateAsync(null, null, environmentOptions);
            await webViewVideo.EnsureCoreWebView2Async(environment);
            
            // Tam ekran yapıldığında tetiklenecek olan event
            webViewVideo.CoreWebView2.ContainsFullScreenElementChanged += CoreWebView2_ContainsFullScreenElementChanged;

            Log("Dahili Video Oynatıcı (WebView2) Türkçe kontrollerle hazır.");
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
            // Videoda tam ekran butonuna basıldığında
            _previousWindowState = this.WindowState;
            _previousBorderStyle = this.FormBorderStyle;
            _previousParent = webViewVideo.Parent;

            // Formun kenarlıklarını kaldırıp tam ekrana geç
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            // WebView2 kontrolünü TabControl'den çıkartıp doğrudan ekrana (Form'a) yasla
            webViewVideo.Parent = this;
            webViewVideo.BringToFront();
        }
        else
        {
            // Tam ekrandan (ESC) çıkıldığında eski haline döndür
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
            _currentStory = new Story { Prompt = txtPrompt.Text };
            
            Log("Hikaye LLM ile oluşturuluyor...");
            _currentStory.Content = await _llmService.GenerateStoryAsync(txtPrompt.Text);
            txtStoryContent.Text = _currentStory.Content;

            Log("DALL-E ile 3 adet görsel üretiliyor...");
            _currentStory.ImagePaths = await _imageService.GenerateImagesAsync(_currentStory.Content, 3);
            
            flowLayoutPanelImages.Controls.Clear();
            foreach (var path in _currentStory.ImagePaths)
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
            
            tabControlMedia.SelectedTab = tabPageImages; // Görseller sekmesini aktif et

            Log("TTS ile hikaye seslendiriliyor...");
            _currentStory.AudioPath = await _ttsService.GenerateAudioAsync(_currentStory.Content);

            Log("Kayıt veritabanına ekleniyor...");
            await _dbService.SaveStoryAsync(_currentStory);

            Log("1. Aşama tamamlandı! Artık video oluşturabilirsiniz.");
            btnGenerateVideo.Enabled = true;
        }
        catch (Exception ex)
        {
            Log($"Hata: {ex.Message}");
            MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            Log("FFmpeg ile video işleniyor (Görseller, Geçiş efektleri, Ses, Altyazı)... Bu işlem biraz zaman alabilir.");
            
            _currentStory.VideoPath = await _videoService.CreateVideoAsync(
                _currentStory.ImagePaths, 
                _currentStory.AudioPath, 
                _currentStory.Content);

            Log("Video başarıyla oluşturuldu! Yol: " + _currentStory.VideoPath);
            await _dbService.SaveStoryAsync(_currentStory); 
            
            Log("Video, dahili oynatıcıda açılıyor...");
            tabControlMedia.SelectedTab = tabPageVideo; // Video sekmesini aktif et
            
            try 
            {
                // Videoyu WebView2 içerisinde oynatmak için mutlak yol (URI) kullanıyoruz
                string videoUri = new Uri(_currentStory.VideoPath).AbsoluteUri;
                webViewVideo.CoreWebView2.Navigate(videoUri);
            } 
            catch (Exception ex) {
                Log("Video oynatıcı hatası: " + ex.Message);
            }
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
        if (string.IsNullOrWhiteSpace(txtStoryContent.Text))
        {
            return;
        }

        try
        {
            btnSpeak.Enabled = false;
            Log("Metin seslendiriliyor...");

            // Eğer daha önce ses oluşturulmamışsa veya metin değişmişse (opsiyonel basit kontrol)
            // burada doğrudan yeni bir ses dosyası üretip çalıyoruz.
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