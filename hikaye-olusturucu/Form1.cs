using System;
using System.Drawing;
using System.Windows.Forms;
using hikaye_olusturucu.Core.Interfaces;
using hikaye_olusturucu.Core.Models;

namespace hikaye_olusturucu;

public partial class Form1 : Form
{
    private readonly ILLMService _llmService;
    private readonly IImageGenerationService _imageService;
    private readonly ITtsService _ttsService;
    private readonly IVideoService _videoService;
    private readonly IDatabaseService _dbService;

    private Story _currentStory = new();

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
        Log("Sistem baÅŸlatÄ±lÄ±yor...");
        try 
        {
            await _dbService.InitializeDatabaseAsync();
            Log("VeritabanÄ± hazÄ±r.");
        }
        catch (Exception ex)
        {
            Log("VeritabanÄ± hatasÄ±: " + ex.Message);
        }
    }

    private async void btnGenerateStory_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPrompt.Text))
        {
            MessageBox.Show("LÃ¼tfen bir prompt girin.");
            return;
        }

        try
        {
            ToggleUI(false);
            _currentStory = new Story { Prompt = txtPrompt.Text };
            
            Log("Hikaye LLM ile oluÅŸturuluyor...");
            _currentStory.Content = await _llmService.GenerateStoryAsync(txtPrompt.Text);
            txtStoryContent.Text = _currentStory.Content;

            Log("DALL-E ile 3 adet gÃ¶rsel Ã¼retiliyor...");
            _currentStory.ImagePaths = await _imageService.GenerateImagesAsync(_currentStory.Content, 3);
            
            flowLayoutPanelImages.Controls.Clear();
            foreach (var path in _currentStory.ImagePaths)
            {
                var pb = new PictureBox
                {
                    Image = Image.FromFile(path),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width = 340,
                    Height = 340,
                    Margin = new Padding(5)
                };
                flowLayoutPanelImages.Controls.Add(pb);
            }

            Log("TTS ile hikaye seslendiriliyor...");
            _currentStory.AudioPath = await _ttsService.GenerateAudioAsync(_currentStory.Content);

            Log("KayÄ±t veritabanÄ±na ekleniyor...");
            await _dbService.SaveStoryAsync(_currentStory);

            Log("1. AÅŸama tamamlandÄ±! ArtÄ±k video oluÅŸturabilirsiniz.");
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
            Log("FFmpeg ile video iÅŸleniyor (GÃ¶rseller, GeÃ§iÅŸ efektleri, Ses, AltyazÄ±)... Bu iÅŸlem biraz zaman alabilir.");
            
            _currentStory.VideoPath = await _videoService.CreateVideoAsync(
                _currentStory.ImagePaths, 
                _currentStory.AudioPath, 
                _currentStory.Content);

            Log("Video baÅŸarÄ±yla oluÅŸturuldu! Yol: " + _currentStory.VideoPath);
            await _dbService.SaveStoryAsync(_currentStory); 
            
            MessageBox.Show($"Video oluÅŸturuldu!\nDosya: {_currentStory.VideoPath}", "BaÅŸarÄ±lÄ±", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            try 
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_currentStory.VideoPath}\"");
            } 
            catch { }
        }
        catch (Exception ex)
        {
            Log($"Video HatasÄ±: {ex.Message}");
            MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleUI(true);
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