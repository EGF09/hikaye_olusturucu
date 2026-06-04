using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        string systemPrompt = "Sen yaratıcı ve profesyonel bir Türkçe yazarısın. Hikaye tam olarak 3 paragraftan oluşmalıdır (Giriş, Gelişme, Sonuç). " +
                             "HİÇBİR ŞEKİLDE \"Giriş:\", \"Gelişme:\", \"Sonuç:\" veya \"1. Paragraf:\", \"Başlangıç:\" gibi bölüm başlıkları, etiketler veya numaralandırmalar ekleme. " +
                             "Sadece doğrudan hikayenin paragraflarını yaz. Paragraflar arasında tam olarak birer boş satır bırak. " +
                             "Hikaye tamamen Türkçe olmalı ve çince, japonca gibi yabancı/Asya dillerinden hiçbir karakter veya kelime içermemelidir.";
        string fullPrompt = $"{systemPrompt}\n\nKonu: {prompt}";

        string rawStory = await GenerateTextContentAsync(fullPrompt);
        return CleanStoryText(rawStory);
    }

    private string CleanStoryText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1. Çince, Japonca, Korece karakterleri ve sembolleri temizle (Hiragana, Katakana, Kanji, Hangul vb.)
        text = Regex.Replace(text, @"[\u3000-\u303f\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u4e00-\u9faf\u4e00-\u9fff\u3400-\u4dbf\uac00-\ud7af]", "");

        // 2. Paragrafları satır bazlı ayırıp başlıkları temizle
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var processedParagraphs = new List<string>();

        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Bölüm başlıklarını ve etiketleri temizle: Örn: **Giriş:**, Giriş - , 1. Gelişme: vb.
            string cleanLine = Regex.Replace(
                trimmed, 
                @"^(?i)[\s*_#-]*([0-9]+[. \s-]*)?(giriş|gelişme|sonuç|paragraf|bölüm|sahne|adım|step|intro|body|conclusion|giris|gelisme|sonuc)\s*([0-9]+)?\s*(:\s*|--?\s*|\.\s*|\*\*\s*|\s*$)", 
                ""
            );

            // Yalnızca sayısal listeleri temizle: Örn: 1., 2., 1- vb.
            cleanLine = Regex.Replace(
                cleanLine, 
                @"^(?i)[\s*_#-]*[0-9]+[. \s-]*(:\s*|--?\s*|\.\s*|\s*$)", 
                ""
            );

            // Kenarlardaki artık markdown işaretlerini veya noktalama işaretlerini temizle
            // Sol taraftan nokta ve diğer semboller silinebilir, ancak sağ taraftan (cümle sonundan) nokta silinmemelidir.
            cleanLine = Regex.Replace(cleanLine, @"^[*\s_#:\.-]+", "");
            cleanLine = Regex.Replace(cleanLine, @"[*\s_#:-]+$", "");

            if (!string.IsNullOrWhiteSpace(cleanLine))
            {
                // Birden fazla boşluğu teke indirge
                cleanLine = Regex.Replace(cleanLine, @"\s+", " ");
                processedParagraphs.Add(cleanLine);
            }
        }

        // 3. Paragraf sayısını tam olarak 3'e eşitle (Giriş, Gelişme, Sonuç)
        if (processedParagraphs.Count == 2)
        {
            // 2 paragraf varsa, daha uzun olanını cümle sınırından ikiye böl
            int indexToSplit = processedParagraphs[0].Length >= processedParagraphs[1].Length ? 0 : 1;
            string targetPara = processedParagraphs[indexToSplit];
            var sentences = Regex.Split(targetPara, @"(?<=[.!?])\s+");
            if (sentences.Length > 1)
            {
                int splitPoint = (int)Math.Ceiling(sentences.Length / 2.0);
                string part1 = string.Join(" ", sentences.Take(splitPoint));
                string part2 = string.Join(" ", sentences.Skip(splitPoint));
                
                processedParagraphs.RemoveAt(indexToSplit);
                processedParagraphs.Insert(indexToSplit, part1);
                processedParagraphs.Insert(indexToSplit + 1, part2);
            }
        }
        else if (processedParagraphs.Count > 3)
        {
            // 3'ten fazla paragraf varsa, 3 paragraf kalana kadar son paragrafları birleştir
            while (processedParagraphs.Count > 3)
            {
                string merged = processedParagraphs[processedParagraphs.Count - 2] + " " + processedParagraphs[processedParagraphs.Count - 1];
                processedParagraphs.RemoveAt(processedParagraphs.Count - 1);
                processedParagraphs[processedParagraphs.Count - 1] = merged;
            }
        }
        else if (processedParagraphs.Count == 1)
        {
            // Tek bir devasa paragraf varsa, bunu cümle sınırlarından 3'e böl
            string targetPara = processedParagraphs[0];
            var sentences = Regex.Split(targetPara, @"(?<=[.!?])\s+");
            if (sentences.Length >= 3)
            {
                int size = (int)Math.Ceiling(sentences.Length / 3.0);
                string part1 = string.Join(" ", sentences.Take(size));
                string part2 = string.Join(" ", sentences.Skip(size).Take(size));
                string part3 = string.Join(" ", sentences.Skip(size * 2));
                
                processedParagraphs.Clear();
                processedParagraphs.Add(part1);
                processedParagraphs.Add(part2);
                processedParagraphs.Add(part3);
            }
        }

        // Paragrafları çift satır boşluğuyla birleştir
        return string.Join("\r\n\r\n", processedParagraphs);
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
        for (int i = 0; i < count; i++)
        {
            try
            {
                string currentPrompt = i < prompts.Count ? prompts[i] : $"Cinematic digital art of: {storyContent.Substring(0, Math.Min(storyContent.Length, 100))}";
                if (currentPrompt.Length > 350)
                {
                    currentPrompt = currentPrompt.Substring(0, 350);
                }

                byte[] imageBytes = null;
                bool isAiGenerated = false;

                // 1. Pollinations AI (Anahtarlı - Flux)
                bool hasPollinationsKey = !string.IsNullOrWhiteSpace(_pollinationsApiKey) && !_pollinationsApiKey.Contains("YOUR_POLLINATIONS_API_KEY");
                if (hasPollinationsKey)
                {
                    imageBytes = await CallPollinationsWithRetry(currentPrompt, "flux", _pollinationsApiKey);
                    if (imageBytes != null && imageBytes.Length > 1000) isAiGenerated = true;
                }

                // 2. Pollinations AI (Anahtarsız - Flux)
                if (imageBytes == null || imageBytes.Length <= 1000)
                {
                    imageBytes = await CallPollinationsWithRetry(currentPrompt, "flux");
                    if (imageBytes != null && imageBytes.Length > 1000) isAiGenerated = true;
                }

                // 3. Pollinations AI (Anahtarsız - Turbo)
                if (imageBytes == null || imageBytes.Length <= 1000)
                {
                    imageBytes = await CallPollinationsWithRetry(currentPrompt, "turbo");
                    if (imageBytes != null && imageBytes.Length > 1000) isAiGenerated = true;
                }

                // 4. Pollinations AI (Anahtarsız - Anime)
                if (imageBytes == null || imageBytes.Length <= 1000)
                {
                    imageBytes = await CallPollinationsWithRetry(currentPrompt, "anime");
                    if (imageBytes != null && imageBytes.Length > 1000) isAiGenerated = true;
                }

                // 5. Hugging Face (Anahtarlı - Flux Schnell)
                bool hasHF = !string.IsNullOrWhiteSpace(_huggingFaceApiKey) && !_huggingFaceApiKey.Contains("YOUR_HF_API_KEY");
                if (hasHF && (imageBytes == null || imageBytes.Length <= 1000))
                {
                    imageBytes = await CallHuggingFaceWithRetry(currentPrompt, "black-forest-labs/FLUX.1-schnell");
                    if (imageBytes != null && imageBytes.Length > 1000) isAiGenerated = true;
                }

                // 6. Hugging Face (Anahtarlı - SDXL)
                if (hasHF && (imageBytes == null || imageBytes.Length <= 1000))
                {
                    imageBytes = await CallHuggingFaceWithRetry(currentPrompt, "stabilityai/stable-diffusion-xl-base-1.0");
                    if (imageBytes != null && imageBytes.Length > 1000) isAiGenerated = true;
                }

                // 7. Hugging Face (Anahtarlı - SD 1.5)
                if (hasHF && (imageBytes == null || imageBytes.Length <= 1000))
                {
                    imageBytes = await CallHuggingFaceWithRetry(currentPrompt, "runwayml/stable-diffusion-v1-5");
                    if (imageBytes != null && imageBytes.Length > 1000) isAiGenerated = true;
                }

                // Kaydetme ve Gecikme
                if (imageBytes != null && imageBytes.Length > 1000)
                {
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"image_{Guid.NewGuid()}.png");
                    await File.WriteAllBytesAsync(filePath, imageBytes);
                    imagePaths.Add(filePath);

                    // Sıralı isteklerde IP bazlı eşzamanlılık kuyruğunun dolmaması için 10 saniye bekle
                    if (isAiGenerated && i < count - 1)
                    {
                        await Task.Delay(10000);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Döngü içi görsel oluşturma hatası: {ex.Message}");
            }
        }

        // 3. Fallback: Tüm AI servisleri başarısız olduysa estetik, tema uyumlu bir konsept yer tutucu kartı oluştur.
        while (imagePaths.Count < count)
        {
            int i = imagePaths.Count;
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"fallback_image_{Guid.NewGuid()}.png");
            
            using (var bitmap = new System.Drawing.Bitmap(1024, 1024))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    
                    // 1. Premium Arka Plan Gradiyenti (Karanlık Lale/Gece Mavisi Tonları)
                    using (var gradBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new System.Drawing.Rectangle(0, 0, 1024, 1024),
                        System.Drawing.Color.FromArgb(17, 17, 27),     // Çok koyu lacivert
                        System.Drawing.Color.FromArgb(49, 50, 68),     // Koyu gri/lila
                        45f))
                    {
                        graphics.FillRectangle(gradBrush, 0, 0, 1024, 1024);
                    }

                    // 2. Estetik Soyut Daireler (Arka plan süslemesi)
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(20, 137, 180, 250), 3)) // Şeffaf Akant Mavisi
                    {
                        graphics.DrawEllipse(pen, 112, 112, 800, 800);
                        graphics.DrawEllipse(pen, 212, 212, 600, 600);
                        graphics.DrawEllipse(pen, 312, 312, 400, 400);
                    }

                    // 3. İç Çerçeve
                    using (var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 203, 166, 247), 4)) // Şeffaf Lavanta
                    {
                        graphics.DrawRectangle(borderPen, 40, 40, 944, 944);
                    }

                    // 4. Şık Tipografi
                    using (var titleFont = new System.Drawing.Font("Segoe UI", 42, System.Drawing.FontStyle.Bold))
                    using (var subFont = new System.Drawing.Font("Segoe UI", 24, System.Drawing.FontStyle.Regular))
                    using (var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(205, 214, 244))) // Açık Gri/Beyaz
                    using (var accentBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(203, 166, 247))) // Lavanta
                    {
                        var format = new System.Drawing.StringFormat 
                        { 
                            Alignment = System.Drawing.StringAlignment.Center, 
                            LineAlignment = System.Drawing.StringAlignment.Center 
                        };

                        // Üst etiket
                        graphics.DrawString("YAPAY ZEKA GÖRSEL SERVİSİ", subFont, accentBrush, new System.Drawing.RectangleF(0, 330, 1024, 80), format);
                        
                        // Ana Başlık
                        graphics.DrawString("Görsel Yüklenemedi", titleFont, textBrush, new System.Drawing.RectangleF(0, 410, 1024, 120), format);
                        
                        // Alt açıklama
                        string subText = $"Sahne {i + 1}\n(Bağlantı yoğunluğu nedeniyle tasarım hazırlanamadı)";
                        graphics.DrawString(subText, subFont, textBrush, new System.Drawing.RectangleF(0, 530, 1024, 150), format);
                    }
                }
                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }
            imagePaths.Add(filePath);
        }

        return imagePaths;
    }

    private async Task<byte[]> CallPollinationsWithRetry(string prompt, string model, string apiKey = "")
    {
        int maxRetries = 2;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var bytes = await CallPollinationsImageApi(prompt, model, apiKey);
                if (bytes != null && bytes.Length > 1000)
                {
                    return bytes;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pollinations retry {attempt} failed: {ex.Message}");
            }
            if (attempt < maxRetries - 1)
            {
                await Task.Delay(4000);
            }
        }
        return null;
    }

    private async Task<byte[]> CallHuggingFaceWithRetry(string prompt, string model)
    {
        int maxRetries = 2;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var bytes = await CallHuggingFaceImageApi(prompt, model);
                if (bytes != null && bytes.Length > 1000)
                {
                    return bytes;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hugging Face retry {attempt} failed: {ex.Message}");
            }
            if (attempt < maxRetries - 1)
            {
                await Task.Delay(4000);
            }
        }
        return null;
    }

    private async Task<byte[]> CallPollinationsImageApi(string prompt, string model, string apiKey = "")
    {
        string seed = Guid.NewGuid().GetHashCode().ToString();
        string url;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            url = $"https://gen.pollinations.ai/image/{Uri.EscapeDataString(prompt)}?width=1024&height=1024&nologo=true&seed={seed}&model={model}&key={Uri.EscapeDataString(apiKey)}";
        }
        else
        {
            url = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString(prompt)}?width=1024&height=1024&nologo=true&seed={seed}&model={model}";
        }

        using (var cleanClient = new HttpClient())
        {
            cleanClient.Timeout = TimeSpan.FromSeconds(25);
            return await cleanClient.GetByteArrayAsync(url);
        }
    }

    private async Task<byte[]> CallHuggingFaceImageApi(string prompt, string model)
    {
        string url = $"https://api-inference.huggingface.co/models/{model}";
        var requestBody = new { inputs = prompt };
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _huggingFaceApiKey);
        requestMessage.Content = content;

        var response = await _httpClient.SendAsync(requestMessage);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }
        return null;
    }
}
