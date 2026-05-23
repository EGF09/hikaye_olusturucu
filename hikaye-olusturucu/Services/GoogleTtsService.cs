using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using hikaye_olusturucu.Core.Interfaces;

namespace hikaye_olusturucu.Services;

public class GoogleTtsService : ITtsService
{
    private readonly HttpClient _httpClient;

    public GoogleTtsService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<string> GenerateAudioAsync(string text)
    {
        var chunks = SplitText(text, 200);
        using var ms = new MemoryStream();
        
        foreach (var chunk in chunks)
        {
            string encodedText = Uri.EscapeDataString(chunk);
            string url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={encodedText}&tl=tr&client=tw-ob";

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"TTS API Hatası ({response.StatusCode}): {errorBody}");
            }

            var audioBytes = await response.Content.ReadAsByteArrayAsync();
            ms.Write(audioBytes, 0, audioBytes.Length);
            
            // API sınırlandırmalarına takılmamak için kısa bir bekleme süresi
            await Task.Delay(100);
        }

        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"audio_{Guid.NewGuid()}.mp3");
        await File.WriteAllBytesAsync(filePath, ms.ToArray());

        return filePath;
    }

    private List<string> SplitText(string text, int maxChunkSize)
    {
        var chunks = new List<string>();
        var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();

        foreach (var word in words)
        {
            if (currentChunk.Length + word.Length + 1 > maxChunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
                
                if (word.Length > maxChunkSize)
                {
                    string longWord = word;
                    while (longWord.Length > maxChunkSize)
                    {
                        chunks.Add(longWord.Substring(0, maxChunkSize));
                        longWord = longWord.Substring(maxChunkSize);
                    }
                    currentChunk.Append(longWord).Append(" ");
                }
                else
                {
                    currentChunk.Append(word).Append(" ");
                }
            }
            else
            {
                currentChunk.Append(word).Append(" ");
            }
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
}