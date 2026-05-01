using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hikaye_olusturucu.Core.Interfaces;

namespace hikaye_olusturucu.Services;

public class FFmpegVideoService : IVideoService
{
    private readonly string _ffmpegPath;

    public FFmpegVideoService(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }

    public async Task<string> CreateVideoAsync(List<string> imagePaths, string audioPath, string storyContent)
    {
        if (imagePaths.Count == 0) throw new Exception("GÃ¶rsel bulunamadÄ±.");

        string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"video_{Guid.NewGuid()}.mp4");
        string subtitlePath = CreateSubtitleFile(storyContent);
        
        // AltyazÄ± dosyasÄ± FFmpeg filter syntax'Ä±na uygun formatlanmalÄ±
        string srtPath = subtitlePath.Replace("\\", "/").Replace(":", "\\:");

        int durationPerImage = 5; // Her gÃ¶rsel 5 saniye
        int fadeDuration = 1;     // 1 saniyelik geÃ§iÅŸ efekti (fade/xfade)

        var sbInputs = new StringBuilder();
        var sbFilters = new StringBuilder();

        // Her gÃ¶rseli input olarak ekle
        for (int i = 0; i < imagePaths.Count; i++)
        {
            sbInputs.Append($"-loop 1 -t {durationPerImage} -i \"{imagePaths[i]}\" ");
            // format=yuv420p yaparak donanÄ±msal oynatÄ±cÄ±lara uyumluluÄŸu saÄŸlÄ±yoruz (Ã¶nceden yuva420p alfa kanallÄ±ydÄ±)
            sbFilters.Append($"[{i}:v]scale=1024:1024,trim=duration={durationPerImage},format=yuv420p[v{i}]; ");
        }

        string lastNode = "[v0]";
        int currentOffset = durationPerImage - fadeDuration;

        // GeÃ§iÅŸ efektleri (xfade) oluÅŸtur
        for (int i = 1; i < imagePaths.Count; i++)
        {
            string nextNode = $"[v{i}]";
            string outNode = $"[out{i}]";
            sbFilters.Append($"{lastNode}{nextNode}xfade=transition=fade:duration={fadeDuration}:offset={currentOffset}{outNode}; ");
            lastNode = outNode;
            currentOffset += (durationPerImage - fadeDuration);
        }

        // Subtitles filter'Ä±nÄ± en son node'a uygula
        sbFilters.Append($"{lastNode}subtitles='{srtPath}':force_style='FontSize=24,PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000'[finalv]");

        // FFmpeg argÃ¼manlarÄ±nÄ± birleÅŸtir. Sesi de input olarak ver (-i audio)
        // -pix_fmt yuv420p eklenerek Ã§Ä±ktÄ± dosyasÄ±nÄ±n Windows Medya OynatÄ±cÄ±sÄ± ile tamamen uyumlu olmasÄ± zorlandÄ±.
        string arguments = $"-y {sbInputs} -i \"{audioPath}\" -filter_complex \"{sbFilters}\" -map \"[finalv]\" -map {imagePaths.Count}:a -c:v libx264 -pix_fmt yuv420p -c:a aac -shortest \"{outputPath}\"";

        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process != null)
        {
            var errorTask = process.StandardError.ReadToEndAsync();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            await Task.WhenAll(errorTask, outputTask, process.WaitForExitAsync());

            if (process.ExitCode != 0)
            {
                string error = await errorTask;
                throw new Exception($"FFmpeg hatasÄ±: {error}\nKullanÄ±lan ArgÃ¼manlar: {arguments}");
            }
        }

        return outputPath;
    }

    private string CreateSubtitleFile(string content)
    {
        string srtPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"subs_{Guid.NewGuid()}.srt");
        var words = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        int wordsPerSubtitle = 8;
        int index = 1;
        
        var srtBuilder = new StringBuilder();

        for (int i = 0; i < words.Length; i += wordsPerSubtitle)
        {
            var chunk = string.Join(" ", words.Skip(i).Take(wordsPerSubtitle));
            TimeSpan start = TimeSpan.FromSeconds((index - 1) * 3);
            TimeSpan end = TimeSpan.FromSeconds(index * 3);

            srtBuilder.AppendLine(index.ToString());
            srtBuilder.AppendLine($"{start:hh\\:mm\\:ss\\,fff} --> {end:hh\\:mm\\:ss\\,fff}");
            srtBuilder.AppendLine(chunk);
            srtBuilder.AppendLine();
            index++;
        }

        File.WriteAllText(srtPath, srtBuilder.ToString());
        return srtPath;
    }
}