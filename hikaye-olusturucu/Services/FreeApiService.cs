using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using hikaye_olusturucu.Core.Interfaces;
using Windows.Media.SpeechSynthesis;

namespace hikaye_olusturucu.Services;

public class FreeApiService : ILLMService, IImageGenerationService, ITtsService
{
    private readonly HttpClient _httpClient;

    public FreeApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<string> GenerateStoryAsync(string prompt)
    {
        string systemPrompt = "Sen yaratıcı bir hikaye yazarısın. Sadece hikaye metnini döndür. Teknik açıklamalar ekleme. Hikayeyi kısa (maksimum 3 paragraf), etkileyici ve görsel olarak betimlenebilecek şekilde Türkçe yaz.";
        string fullPrompt = $"{systemPrompt}\n\nKonu: {prompt}";

        return await CallTextApi(fullPrompt);
    }

    public async Task<string> GenerateTitleAsync(string storyContent)
    {
        string prompt = $"Aşağıdaki hikaye için maksimum 4 kelimelik etkileyici bir başlık yaz. Sadece başlığı döndür: {storyContent}";
        string title = await CallTextApi(prompt);
        return title.Trim().Trim('"').Trim('.');
    }

    private async Task<string> CallTextApi(string prompt)
    {
        // Denenecek model listesi
        string[] models = { "openai", "mistral", "p1", "llama", "qwen", "gemini" };
        List<string> errors = new List<string>();

        foreach (var model in models)
        {
            try
            {
                // POST isteği daha güvenilirdir ve URL uzunluk sınırına takılmaz
                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    model = model,
                    cache = false,
                    jsonMode = false
                };

                string jsonContext = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContext, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://text.pollinations.ai/", content);
                
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(result)) return result;
                }
                
                errors.Add($"{model}: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                errors.Add($"{model}: {ex.Message}");
            }
        }

        throw new Exception($"Servis yoğunluğu veya bağlantı sorunu. Lütfen tekrar deneyin. (Hatalar: {string.Join(", ", errors)})");
    }

    public async Task<List<string>> GenerateImagesAsync(string storyContent, int count = 3)
    {
        var imagePaths = new List<string>();
        string basePrompt = $"Cinematic, highly detailed digital art: {storyContent.Substring(0, Math.Min(storyContent.Length, 250))}";

        for (int i = 0; i < count; i++)
        {
            try
            {
                string prompt = $"{basePrompt} - Part {i + 1}";
                string url = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString(prompt)}?width=1024&height=1024&nologo=true&seed={Guid.NewGuid().GetHashCode()}";

                var imageBytes = await _httpClient.GetByteArrayAsync(url);
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"image_{Guid.NewGuid()}.png");
                await File.WriteAllBytesAsync(filePath, imageBytes);
                imagePaths.Add(filePath);
            }
            catch { continue; }
        }
        
        if (imagePaths.Count == 0) throw new Exception("Görsel servisi şu an yanıt vermiyor.");
        return imagePaths;
    }

    public async Task<string> GenerateAudioAsync(string text)
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"audio_{Guid.NewGuid()}.wav");   
        using (var synthesizer = new SpeechSynthesizer())
        {
            var voices = SpeechSynthesizer.AllVoices;
            var trVoice = voices.FirstOrDefault(v => v.DisplayName.Contains("Tolga")) ??
                          voices.FirstOrDefault(v => v.Language.StartsWith("tr", StringComparison.OrdinalIgnoreCase));
            if (trVoice != null) synthesizer.Voice = trVoice;

            using (var stream = await synthesizer.SynthesizeTextToStreamAsync(text))
            using (var fileStream = File.Create(filePath))
            using (var reader = stream.AsStreamForRead())
            {
                await reader.CopyToAsync(fileStream);
            }
        }
        return filePath;
    }
}
