using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
    }

    public async Task<string> GenerateStoryAsync(string prompt)
    {
        string systemPrompt = "Sen yaratıcı bir hikaye yazarısın. Sadece hikaye metnini döndür. Teknik açıklamalar, 'paragraf sınırı doldu' gibi notlar veya gereksiz bilgiler ekleme. Hikayeyi kısa (maksimum 3 paragraf), etkileyici ve görsel olarak betimlenebilecek şekilde yaz ve mutlaka bir sonuca bağlayarak bitir.";
        string fullPrompt = $"{systemPrompt} Konu: {prompt}";
        
        string url = $"https://text.pollinations.ai/{Uri.EscapeDataString(fullPrompt)}?model=openai";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GenerateTitleAsync(string storyContent)
    {
        string prompt = $"Aşağıdaki hikaye için çok kısa (maksimum 4 kelime), etkileyici bir başlık yaz. Sadece başlığı döndür, tırnak işareti veya nokta kullanma: {storyContent}";
        string url = $"https://text.pollinations.ai/{Uri.EscapeDataString(prompt)}?model=openai";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string title = await response.Content.ReadAsStringAsync();
        return title.Trim().Trim('"').Trim('.');
    }

    public async Task<List<string>> GenerateImagesAsync(string storyContent, int count = 3)
    {
        var imagePaths = new List<string>();
        string basePrompt = $"A highly detailed cinematic illustration of this story: {storyContent.Substring(0, Math.Min(storyContent.Length, 300))}";

        for (int i = 0; i < count; i++)
        {
            string prompt = $"{basePrompt} - Scene part {i + 1}. No text in the image.";
            string url = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString(prompt)}?width=1024&height=1024&nologo=true&seed={Guid.NewGuid().GetHashCode()}";
            
            var imageBytes = await _httpClient.GetByteArrayAsync(url);
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"image_{Guid.NewGuid()}.png");
            await File.WriteAllBytesAsync(filePath, imageBytes);
            imagePaths.Add(filePath);
        }
        return imagePaths;
    }

    public async Task<string> GenerateAudioAsync(string text)
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"audio_{Guid.NewGuid()}.wav");
        
        using (var synthesizer = new SpeechSynthesizer())
        {
            var voices = SpeechSynthesizer.AllVoices;
            // İlk olarak Tolga'yı ara, yoksa Türkçe arayüzlü bir ses seç
            var trVoice = voices.FirstOrDefault(v => v.DisplayName.Contains("Tolga")) ?? 
                          voices.FirstOrDefault(v => v.Language.StartsWith("tr", StringComparison.OrdinalIgnoreCase));
            
            if (trVoice != null)
            {
                synthesizer.Voice = trVoice;
            }

            using (var stream = await synthesizer.SynthesizeTextToStreamAsync(text))
            {
                using (var fileStream = File.Create(filePath))
                {
                    using (var reader = stream.AsStreamForRead())
                    {
                        await reader.CopyToAsync(fileStream);
                    }
                }
            }
        }
        
        return filePath;
    }
}