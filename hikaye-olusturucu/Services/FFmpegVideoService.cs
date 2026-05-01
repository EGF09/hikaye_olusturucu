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

    private double GetAudioDuration(string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            using var reader = new BinaryReader(fs);
            reader.ReadBytes(12); // RIFF, size, WAVE
            while (fs.Position < fs.Length)
            {
                var chunkId = new string(reader.ReadChars(4));
                var chunkSize = reader.ReadInt32();
                if (chunkId == "fmt ")
                {
                    reader.ReadInt16(); // format tag
                    reader.ReadInt16(); // channels
                    reader.ReadInt32(); // sample rate
                    int byteRate = reader.ReadInt32(); // byte rate
                    fs.Position += chunkSize - 12; // skip rest of fmt
                    
                    // Now find data chunk
                    while (fs.Position < fs.Length)
                    {
                        chunkId = new string(reader.ReadChars(4));
                        chunkSize = reader.ReadInt32();
                        if (chunkId == "data")
                        {
                            return (double)chunkSize / byteRate;
                        }
                        fs.Position += chunkSize;
                    }
                }
                else
                {
                    fs.Position += chunkSize;
                }
            }
        }
        catch { }
        return 15.0; // Fallback
    }

    public async Task<string> CreateVideoAsync(List<string> imagePaths, string audioPath, string storyContent)
    {
        if (imagePaths.Count == 0) throw new Exception("Görsel bulunamadı.");

        double totalAudioDuration = GetAudioDuration(audioPath);
        double fadeDuration = 1.0;
        double durationPerImage = ((totalAudioDuration - fadeDuration) / imagePaths.Count) + fadeDuration;

        string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"video_{Guid.NewGuid()}.mp4");
        string subtitlePath = CreateSubtitleFile(storyContent, totalAudioDuration);
        string srtPath = subtitlePath.Replace("\\", "/").Replace(":", "\\:");

        var sbInputs = new StringBuilder();
        var sbFilters = new StringBuilder();

        for (int i = 0; i < imagePaths.Count; i++)
        {
            // Noktayı virgül olmasını engellemek için InvariantCulture kullanıyoruz
            string durStr = durationPerImage.ToString(System.Globalization.CultureInfo.InvariantCulture);
            sbInputs.Append($"-loop 1 -t {durStr} -i \"{imagePaths[i]}\" ");
            sbFilters.Append($"[{i}:v]scale=1024:1024,trim=duration={durStr},format=yuv420p[v{i}]; ");
        }

        string lastNode = "[v0]";
        double currentOffset = durationPerImage - fadeDuration;

        for (int i = 1; i < imagePaths.Count; i++)
        {
            string nextNode = $"[v{i}]";
            string outNode = $"[out{i}]";
            string offsetStr = currentOffset.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string fadeDurStr = fadeDuration.ToString(System.Globalization.CultureInfo.InvariantCulture);
            
            sbFilters.Append($"{lastNode}{nextNode}xfade=transition=fade:duration={fadeDurStr}:offset={offsetStr}{outNode}; ");
            lastNode = outNode;
            currentOffset += (durationPerImage - fadeDuration);
        }

        sbFilters.Append($"{lastNode}subtitles='{srtPath}':force_style='FontSize=24,PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000'[finalv]");

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
                throw new Exception($"FFmpeg hatası: {error}\nKullanılan Argümanlar: {arguments}");
            }
        }

        return outputPath;
    }

    private string CreateSubtitleFile(string content, double totalAudioDurationInSeconds)
    {
        string srtPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"subs_{Guid.NewGuid()}.srt");
        var words = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        int wordsPerSubtitle = 6;
        
        var srtBuilder = new StringBuilder();
        double timePerWord = totalAudioDurationInSeconds / Math.Max(1, words.Length);

        double currentTime = 0;
        int index = 1;

        for (int i = 0; i < words.Length; i += wordsPerSubtitle)
        {
            var chunkWords = words.Skip(i).Take(wordsPerSubtitle).ToArray();
            var chunkStr = string.Join(" ", chunkWords);
            double chunkDuration = chunkWords.Length * timePerWord;
            
            TimeSpan start = TimeSpan.FromSeconds(currentTime);
            TimeSpan end = TimeSpan.FromSeconds(currentTime + chunkDuration);

            srtBuilder.AppendLine(index.ToString());
            srtBuilder.AppendLine($"{start:hh\\:mm\\:ss\\,fff} --> {end:hh\\:mm\\:ss\\,fff}");
            srtBuilder.AppendLine(chunkStr);
            srtBuilder.AppendLine();
            
            currentTime += chunkDuration;
            index++;
        }

        File.WriteAllText(srtPath, srtBuilder.ToString());
        return srtPath;
    }
}