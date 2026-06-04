using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing;
using hikaye_olusturucu.Core.Interfaces;

namespace hikaye_olusturucu.Services;

public class FreeApiService : ILLMService, IImageGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly string _geminiApiKey;
    private readonly string _huggingFaceApiKey;
    private readonly string _pollinationsApiKey;

    public FreeApiService(string geminiApiKey, string huggingFaceApiKey, string pollinationsApiKey = "")
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        
        _geminiApiKey = geminiApiKey?.Trim();
        _huggingFaceApiKey = huggingFaceApiKey?.Trim();
        _pollinationsApiKey = pollinationsApiKey?.Trim();
    }

    public async Task<string> GenerateStoryAsync(string prompt)
    {
        string systemPrompt = "Sen yaratıcı ve profesyonel bir Türkçe yazarısın. Hikaye tam olarak 3 paragraftan oluşmalıdır: 1. paragraf Giriş, 2. paragraf Gelişme, ve 3. paragraf Sonuç bölümünü temsil etmelidir. Her paragraf arasında boş satır bırakılmalı, ekstra açıklama, yorum veya başlık eklenmemelidir.";
        string fullPrompt = $"{systemPrompt}\n\nKonu: {prompt}";

        return await GenerateTextContentAsync(fullPrompt);
    }

    public async Task<string> GenerateTitleAsync(string storyContent)
    {
        string prompt = $"Aşağıdaki hikaye için maksimum 4 kelimelik etkileyici bir başlık yaz. Sadece başlığı döndür: {storyContent}";
        string title = await GenerateTextContentAsync(prompt);
        return title.Trim().Trim('"').Trim('.');
    }

    private async Task<string> GenerateTextContentAsync(string prompt)
    {
        var errors = new List<string>();

        // 1. Google Gemini API (Ana Servis) - 429 hatası için gecikmeli deneme ekli
        bool hasGemini = !string.IsNullOrWhiteSpace(_geminiApiKey) && 
                          !_geminiApiKey.Contains("YOUR_GEMINI_API_KEY");
        if (hasGemini)
        {
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    return await CallGeminiTextApi(prompt);
                }
                catch (Exception ex)
                {
                    errors.Add($"Gemini API Hatası (Deneme {attempt}): {ex.Message}");
                    if (attempt < 2 && ex.Message.Contains("429"))
                    {
                        // 429 (Rate Limit) durumunda 2 saniye bekleyip tekrar dene
                        await Task.Delay(2000);
                    }
                }
            }
        }
        else
        {
            errors.Add("Gemini API: appsettings.json dosyasında API anahtarı yapılandırılmamış veya varsayılan değerde kalmış.");
        }

        // 2. Hugging Face API (Yedek Servis 1)
        bool hasHF = !string.IsNullOrWhiteSpace(_huggingFaceApiKey) && 
                      !_huggingFaceApiKey.Contains("YOUR_HF_API_KEY");
        if (hasHF)
        {
            try
            {
                return await CallHuggingFaceTextApi(prompt);
            }
            catch (Exception ex)
            {
                errors.Add($"Hugging Face API Hatası: {ex.Message}");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_huggingFaceApiKey) || _huggingFaceApiKey.Contains("YOUR_HF_API_KEY"))
            {
                errors.Add("Hugging Face API: API anahtarı yapılandırılmamış.");
            }
            else
            {
                errors.Add("Hugging Face API: Eski veya geçersiz (401) API anahtarı algılandığı için atlandı.");
            }
        }

        // 3. Pollinations AI (Yedek Servis 2) - Çoklu model yedekleme
        string[] pollinationsModels = { "openai", "mistral", "p1", "llama", "qwen", "gemini" };
        foreach (var model in pollinationsModels)
        {
            try
            {
                var fallbackBody = new { messages = new[] { new { role = "user", content = prompt } }, model = model, cache = false };
                var fallbackContent = new StringContent(JsonSerializer.Serialize(fallbackBody), Encoding.UTF8, "application/json");
                var fallbackResponse = await _httpClient.PostAsync("https://text.pollinations.ai/", fallbackContent);
                if (fallbackResponse.IsSuccessStatusCode)
                {
                    string result = await fallbackResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        return result;
                    }
                }
                errors.Add($"Yedek Servis (Pollinations - {model}) Hatası: HTTP {(int)fallbackResponse.StatusCode}");
            }
            catch (Exception ex)
            {
                errors.Add($"Yedek Servis (Pollinations - {model}) Hatası: {ex.Message}");
            }
        }

        // Tüm servisler başarısız olduysa detaylı hata mesajı döndür
        string detailedErrorMessage = "Tüm yapay zeka metin servisleri başarısız oldu:\r\n" + string.Join("\r\n", errors);
        throw new Exception(detailedErrorMessage);
    }

    private async Task<string> CallGeminiTextApi(string prompt)
    {
        string[] geminiModels = { "gemini-flash-latest", "gemini-2.0-flash", "gemini-2.5-flash-lite", "gemini-2.5-flash" };
        List<string> errors = new List<string>();

        foreach (var model in geminiModels)
        {
            try
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(_geminiApiKey)}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = content;

                var response = await _httpClient.SendAsync(requestMessage);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("candidates", out JsonElement candidates) && candidates.GetArrayLength() > 0)
                    {
                        var candidate = candidates[0];
                        if (candidate.TryGetProperty("content", out JsonElement contentEl) &&
                            contentEl.TryGetProperty("parts", out JsonElement parts) &&
                            parts.GetArrayLength() > 0)
                        {
                            return parts[0].GetProperty("text").GetString()?.Trim() ?? "";
                        }
                    }
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    errors.Add($"{model}: Geçersiz API Anahtarı (401 Unauthorized).");
                }
                else
                {
                    errors.Add($"{model}: HTTP {(int)response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{model} Hatası: {ex.Message}");
            }
        }

        throw new Exception(string.Join(" | ", errors));
    }

    private async Task<string> CallHuggingFaceTextApi(string prompt)
    {
        string model = "Qwen/Qwen2.5-7B-Instruct";
        string url = "https://router.huggingface.co/v1/chat/completions";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 1000
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _huggingFaceApiKey);
        requestMessage.Content = content;

        var response = await _httpClient.SendAsync(requestMessage);
        string responseString = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            using JsonDocument doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out JsonElement message) && message.TryGetProperty("content", out JsonElement outContent))
                {
                    return outContent.GetString()?.Trim() ?? "";
                }
            }
        }
        
        throw new HttpRequestException($"HTTP {(int)response.StatusCode}");
    }

    public async Task<List<string>> GenerateImagesAsync(string storyContent, int count = 3)
    {
        var imagePaths = new List<string>();
        List<string> prompts = new List<string>();
        
        // 1. Promptları üret
        try
        {
            string promptGenRequest = $"Aşağıdaki hikaye için görsel oluşturmaya uygun, detaylı ve İngilizce {count} adet prompt hazırla. Her biri hikayenin farklı bir bölümünü temsil etsin. Sadece promptları döndür, numara veya açıklama ekleme. Her prompt yeni satırda olsun.\n\nHikaye: {storyContent}";
            string rawPrompts = await GenerateTextContentAsync(promptGenRequest);
            
            prompts = rawPrompts.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(p => p.Trim().Trim('1', '2', '3', '.', '-', '*', ' ', '"'))
                                   .Where(p => !string.IsNullOrWhiteSpace(p))
                                   .Take(count)
                                   .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Prompt üretimi hatası: {ex.Message}");
        }

        // 2. Görselleri üret
        string hfUrl = "https://api-inference.huggingface.co/models/black-forest-labs/FLUX.1-schnell";

        for (int i = 0; i < count; i++)
        {
            try
            {
                string currentPrompt = i < prompts.Count ? prompts[i] : $"Cinematic, highly detailed digital art, scene {i + 1} of: {storyContent.Substring(0, Math.Min(storyContent.Length, 100))}";
                byte[] imageBytes = null;
                bool isAiGenerated = false;

                // 1. Pollinations AI'ı dene (Anahtarlı)
                bool hasPollinationsKey = !string.IsNullOrWhiteSpace(_pollinationsApiKey) && !_pollinationsApiKey.Contains("YOUR_POLLINATIONS_API_KEY");
                if (hasPollinationsKey)
                {
                    try
                    {
                        string pollinationsUrl = $"https://gen.pollinations.ai/image/{Uri.EscapeDataString(currentPrompt)}?width=1024&height=1024&nologo=true&seed={Guid.NewGuid().GetHashCode()}&key={Uri.EscapeDataString(_pollinationsApiKey)}";
                        imageBytes = await _httpClient.GetByteArrayAsync(pollinationsUrl);
                        if (imageBytes != null && imageBytes.Length > 1000)
                        {
                            isAiGenerated = true;
                        }
                    }
                    catch
                    {
                        imageBytes = null;
                    }
                }

                // 2. Pollinations AI'ı dene (Anahtarsız - Keyless)
                if (imageBytes == null || imageBytes.Length <= 1000)
                {
                    try
                    {
                        using (var cleanClient = new HttpClient())
                        {
                            cleanClient.Timeout = TimeSpan.FromSeconds(25);
                            string seed = Guid.NewGuid().GetHashCode().ToString();
                            string pollinationsUrl = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString(currentPrompt)}?width=1024&height=1024&nologo=true&seed={seed}";
                            imageBytes = await cleanClient.GetByteArrayAsync(pollinationsUrl);
                            if (imageBytes != null && imageBytes.Length > 1000)
                            {
                                isAiGenerated = true;
                            }
                        }
                    }
                    catch
                    {
                        imageBytes = null;
                    }
                }

                // 3. Hugging Face'i dene (Anahtarlı)
                if (imageBytes == null || imageBytes.Length <= 1000)
                {
                    bool hasHF = !string.IsNullOrWhiteSpace(_huggingFaceApiKey) && 
                                  !_huggingFaceApiKey.Contains("YOUR_HF_API_KEY");
                    if (hasHF)
                    {
                        try
                        {
                            var requestBody = new { inputs = currentPrompt };
                            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                            
                            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, hfUrl);
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _huggingFaceApiKey);
                            requestMessage.Content = content;

                            var response = await _httpClient.SendAsync(requestMessage);
                            if (response.IsSuccessStatusCode)
                            {
                                imageBytes = await response.Content.ReadAsByteArrayAsync();
                                if (imageBytes != null && imageBytes.Length > 1000)
                                {
                                    isAiGenerated = true;
                                }
                            }
                        }
                        catch
                        {
                            imageBytes = null;
                        }
                    }
                }

                // 4. LoremFlickr'ı dene (Ücretsiz ve Anahtarsız Tag Bazlı Doğa/Konu Görseli)
                if (imageBytes == null || imageBytes.Length <= 1000)
                {
                    try
                    {
                        string tags = "nature";
                        if (!string.IsNullOrWhiteSpace(currentPrompt))
                        {
                            var words = currentPrompt.Split(' ')
                                .Select(w => new string(w.Where(char.IsLetterOrDigit).ToArray()).ToLower())
                                .Where(w => w.Length > 3 && w != "with" && w != "that" && w != "this" && w != "from" && w != "cute" && w != "high" && w != "detail" && w != "scene")
                                .Take(3)
                                .ToList();
                            if (words.Count > 0)
                            {
                                tags = string.Join(",", words);
                            }
                        }
                        string flickrUrl = $"https://loremflickr.com/1024/1024/{Uri.EscapeDataString(tags)}";
                        using (var cleanClient = new HttpClient())
                        {
                            cleanClient.Timeout = TimeSpan.FromSeconds(20);
                            imageBytes = await cleanClient.GetByteArrayAsync(flickrUrl);
                        }
                    }
                    catch
                    {
                        imageBytes = null;
                    }
                }

                // 5. Picsum Photos'u dene (Ücretsiz ve Anahtarsız Rastgele Doğa/Sanat Görseli)
                if (imageBytes == null || imageBytes.Length <= 1000)
                {
                    try
                    {
                        string picsumUrl = "https://picsum.photos/1024/1024";
                        using (var cleanClient = new HttpClient())
                        {
                            cleanClient.Timeout = TimeSpan.FromSeconds(20);
                            imageBytes = await cleanClient.GetByteArrayAsync(picsumUrl);
                        }
                    }
                    catch
                    {
                        imageBytes = null;
                    }
                }

                // 6. LoremFlickr Random (Ücretsiz ve Anahtarsız Tamamen Rastgele Görsel)
                if (imageBytes == null || imageBytes.Length <= 1000)
                {
                    try
                    {
                        string flickrRandomUrl = "https://loremflickr.com/1024/1024";
                        using (var cleanClient = new HttpClient())
                        {
                            cleanClient.Timeout = TimeSpan.FromSeconds(20);
                            imageBytes = await cleanClient.GetByteArrayAsync(flickrRandomUrl);
                        }
                    }
                    catch
                    {
                        imageBytes = null;
                    }
                }

                if (imageBytes != null && imageBytes.Length > 1000)
                {
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"image_{Guid.NewGuid()}.png");
                    await File.WriteAllBytesAsync(filePath, imageBytes);
                    imagePaths.Add(filePath);

                    // Rate limit koruması: Sadece AI servisleri başarıyla çalıştığında kısa bir bekleme (3sn) ekleyelim
                    if (isAiGenerated && i < count - 1)
                    {
                        await Task.Delay(3000);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Döngü içi görsel oluşturma hatası: {ex.Message}");
            }
        }

        // 3. Fallback: Tüm servisler (AI ve stock) başarısız olduysa boş yer tutucu (placeholder) oluştur.
        while (imagePaths.Count < count)
        {
            int i = imagePaths.Count;
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"fallback_image_{Guid.NewGuid()}.png");
            
            using (var bitmap = new System.Drawing.Bitmap(1024, 1024))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.FromArgb(49, 50, 68)); 
                    using (var font = new System.Drawing.Font("Segoe UI", 48, System.Drawing.FontStyle.Bold))
                    using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(205, 214, 244)))
                    {
                        string text = $"Görsel Üretilemedi\n(API Hatası/Yoğunluğu)\nSahne {i + 1}";
                        var format = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center };
                        graphics.DrawString(text, font, brush, new System.Drawing.RectangleF(0, 0, 1024, 1024), format);
                    }
                }
                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }
            imagePaths.Add(filePath);
        }

        return imagePaths;
    }
}
