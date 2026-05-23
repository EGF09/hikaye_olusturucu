using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using hikaye_olusturucu.Core.Interfaces;

namespace hikaye_olusturucu.Services;

public class FreeApiService : ILLMService, IImageGenerationService
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
        string systemPrompt = "Sen ödüllü ve profesyonel bir Türk yazarısın. Dilbilgisi kurallarına tamamen uygun, akıcı, son derece anlamlı ve mantıklı bir Türkçe hikaye yazacaksın. Kesinlikle uydurma kelimeler, anlamsız harf dizileri veya bozuk cümleler kullanma. Sadece hikaye metnini döndür. Teknik açıklamalar ekleme. Hikayeyi kısa (maksimum 3 paragraf), sürükleyici ve görsel olarak kolayca betimlenebilecek detaylarla yaz.";
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
        
        try 
        {
            // Hikayeden İngilizce görsel promptları türet
            string promptGenRequest = $"Aşağıdaki hikaye için görsel oluşturmaya uygun, detaylı ve İngilizce {count} adet prompt hazırla. Her biri hikayenin farklı bir bölümünü temsil etsin. Sadece promptları döndür, numara veya açıklama ekleme. Her prompt yeni bir satırda olsun.\n\nHikaye: {storyContent}";
            string rawPrompts = await CallTextApi(promptGenRequest);
            var prompts = rawPrompts.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(p => p.Trim().Trim('1', '2', '3', '.', '-', '*', ' ', '"'))
                                   .Where(p => !string.IsNullOrWhiteSpace(p))
                                   .Take(count)
                                   .ToList();

            for (int i = 0; i < count; i++)
            {
                bool success = false;
                for (int attempt = 0; attempt < 2 && !success; attempt++)
                {
                    try
                    {
                        string currentPrompt = i < prompts.Count ? prompts[i] : $"Cinematic, highly detailed digital art, scene {i + 1} of: {storyContent.Substring(0, Math.Min(storyContent.Length, 100))}";
                        string finalPrompt = $"Cinematic, detailed digital art, 8k, realistic lighting, epic composition: {currentPrompt}";
                        string url = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString(finalPrompt)}?width=1024&height=1024&nologo=true&seed={Guid.NewGuid().GetHashCode()}";

                        var imageBytes = await _httpClient.GetByteArrayAsync(url);
                        if (imageBytes.Length < 1000) continue;

                        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"image_{Guid.NewGuid()}.png");
                        await File.WriteAllBytesAsync(filePath, imageBytes);
                        imagePaths.Add(filePath);
                        success = true;
                    }
                    catch { continue; }
                }
            }
        }
        catch 
        {
            // Genel hata durumunda eski basit yönteme dön
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
        }

        if (imagePaths.Count == 0) throw new Exception("Görsel servisi şu an yanıt vermiyor.");
        return imagePaths;
    }
}
