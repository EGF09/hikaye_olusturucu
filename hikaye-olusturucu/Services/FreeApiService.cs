using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using hikaye_olusturucu.Core.Interfaces;

namespace hikaye_olusturucu.Services;

public class FreeApiService : ILLMService, IImageGenerationService, ITtsService
{
    private readonly HttpClient _httpClient;

    public FreeApiService()
    {
        _httpClient = new HttpClient();
        // Uzun sÃ¼rebilecek resim Ã¼retimleri iÃ§in timeout sÃ¼resini artÄ±rÄ±yoruz
        _httpClient.Timeout = TimeSpan.FromMinutes(5); 
    }

    public async Task<string> GenerateStoryAsync(string prompt)
    {
        // Pollinations.ai Text API (Ãœcretsiz, API Key gerektirmez)
        string systemPrompt = "Sen yaratÄ±cÄ± bir hikaye yazarÄ±sÄ±n. Verilen konuya gÃ¶re kÄ±sa, etkileyici ve gÃ¶rsel olarak betimlenebilecek bir hikaye yaz. Maksimum 3 kÄ±sa paragraf olsun.";
        string fullPrompt = $"{systemPrompt} Konu: {prompt}";
        
        string url = $"https://text.pollinations.ai/{Uri.EscapeDataString(fullPrompt)}?model=openai";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<List<string>> GenerateImagesAsync(string storyContent, int count = 3)
    {
        var imagePaths = new List<string>();
        string basePrompt = $"A highly detailed cinematic illustration of this story: {storyContent.Substring(0, Math.Min(storyContent.Length, 300))}";

        for (int i = 0; i < count; i++)
        {
            string prompt = $"{basePrompt} - Scene part {i + 1}. No text in the image.";
            // seed ekleyerek her gÃ¶rselin farklÄ± varyasyonlarda olmasÄ±nÄ± saÄŸlÄ±yoruz
            string url = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString(prompt)}?width=1024&height=1024&nologo=true&seed={Guid.NewGuid().GetHashCode()}";
            
            var imageBytes = await _httpClient.GetByteArrayAsync(url);
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"image_{Guid.NewGuid()}.png");
            await File.WriteAllBytesAsync(filePath, imageBytes);
            imagePaths.Add(filePath);
        }
        return imagePaths;
    }

    public Task<string> GenerateAudioAsync(string text)
    {
        return Task.Run(() =>
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"audio_{Guid.NewGuid()}.wav");
            
            using (var synthesizer = new SpeechSynthesizer())
            {
                bool isTurkishSet = false;
                
                // 1. Ã–ncelik: AdÄ±nda 'Tolga' geÃ§en sesi bul
                foreach (var voice in synthesizer.GetInstalledVoices())
                {
                    if (voice.VoiceInfo.Name.IndexOf("Tolga", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        synthesizer.SelectVoice(voice.VoiceInfo.Name);
                        isTurkishSet = true;
                        break;
                    }
                }

                // 2. Ã–ncelik: Tolga yoksa 'tr' veya 'tr-TR' ile baÅŸlayan herhangi bir TÃ¼rkÃ§e ses bul
                if (!isTurkishSet)
                {
                    foreach (var voice in synthesizer.GetInstalledVoices())
                    {
                        if (voice.VoiceInfo.Culture.Name.StartsWith("tr", StringComparison.OrdinalIgnoreCase) || 
                            voice.VoiceInfo.Culture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase))
                        {
                            synthesizer.SelectVoice(voice.VoiceInfo.Name);
                            isTurkishSet = true;
                            break;
                        }
                    }
                }
                
                synthesizer.SetOutputToWaveFile(filePath);
                synthesizer.Speak(text);
            }
            
            return filePath;
        });
    }
}