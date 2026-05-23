using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hikaye_olusturucu.Core.Interfaces;
using NAudio.Wave;

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
            using var reader = new AudioFileReader(filePath);
            return reader.TotalTime.TotalSeconds;
        }
        catch 
        {
            return 15.0; // Fallback
        }
    }

    public async Task<string> CreateVideoAsync(List<string> imagePaths, string audioPath, string storyContent, string title)
    {
        if (imagePaths.Count == 0) throw new Exception("Görsel bulunamadı.");

        double totalAudioDuration = GetAudioDuration(audioPath);
        double transitionDuration = 0.5; // Geçiş süresi (saniye)

        // Video süresinin ses süresine eşit olması için her görselin süresini ayarlıyoruz
        // n*L - (n-1)*O = totalDuration => L = (totalDuration + (n-1)*O) / n
        double durationPerImage = (totalAudioDuration + (imagePaths.Count - 1) * transitionDuration) / imagePaths.Count;
        
        int fps = 25;
        // zoompan için tam kare sayısı
        int framesPerImage = (int)Math.Round(durationPerImage * fps);

        string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"video_{Guid.NewGuid()}.mp4"); 
        string vttPath = Path.ChangeExtension(outputPath, ".vtt");
        CreateSubtitleFile(storyContent, totalAudioDuration, vttPath);

        var sbInputs = new StringBuilder();
        var sbFilters = new StringBuilder();

        for (int i = 0; i < imagePaths.Count; i++)
        {
            // ÖNEMLİ: -loop 1 KULLANMIYORUZ. Her resmi tek bir kare olarak alıyoruz.
            sbInputs.Append($"-i \"{imagePaths[i]}\" ");

            // zoompan filtresi, tek bir kareyi d={framesPerImage} kadar çoğaltarak video oluşturur.
            // s=1024x1024 çıkış boyutu, fps=25 ise kare hızıdır.
            // x ve y değerleri merkeze odaklanmayı sağlar.
            string zoomEff = (i % 2 == 0) ? "zoom+0.0006" : "1.1-0.0006*on";

            sbFilters.Append($"[{i}:v]scale=2048:2048,zoompan=z='{zoomEff}':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d={framesPerImage}:s=1024x1024:fps={fps},format=yuv420p[v{i}]; ");
        }

        // Xfade geçişleri ile birleştirme
        if (imagePaths.Count > 1)
        {
            string lastStream = "[v0]";
            for (int i = 1; i < imagePaths.Count; i++)
            {
                string nextStream = $"[v{i}]";
                string outStream = (i == imagePaths.Count - 1) ? "[vmerged]" : $"[vm{i}]";
                double offset = i * (durationPerImage - transitionDuration);
                
                sbFilters.Append($"{lastStream}{nextStream}xfade=transition=fade:duration={transitionDuration.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}:offset={offset.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}{outStream}; ");
                
                lastStream = outStream;
            }
        }
        else
        {
            sbFilters.Append("[v0]copy[vmerged]; ");
        }

        // Ses dosyası en son girdi (index = imagePaths.Count)
        string arguments = $"-y {sbInputs} -i \"{audioPath}\" -filter_complex \"{sbFilters}\" -map \"[vmerged]\" -map {imagePaths.Count}:a -c:v libx264 -pix_fmt yuv420p -c:a aac -shortest \"{outputPath}\"";

        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        StringBuilder errorLog = new StringBuilder();
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorLog.AppendLine(e.Data); };

        process.Start();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"FFmpeg Hatası!\n\nDetay:\n{errorLog}\n\nKomut:\n{arguments}");
        }

        return outputPath;
    }

    private string CreateSubtitleFile(string content, double totalAudioDurationInSeconds, string vttPath)
    {
        var words = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        int wordsPerSubtitle = 6;

        var srtBuilder = new StringBuilder();
        srtBuilder.AppendLine("WEBVTT");
        srtBuilder.AppendLine();
        
        // Karakter tabanlı süre hesaplaması (kelime yerine daha isabetli senkronizasyon sağlar)
        int totalChars = string.Join("", words).Length;
        double timePerChar = totalChars > 0 ? totalAudioDurationInSeconds / totalChars : 0;

        double currentTime = 0;

        for (int i = 0; i < words.Length; i += wordsPerSubtitle)
        {
            var chunkWords = words.Skip(i).Take(wordsPerSubtitle).ToArray();
            var chunkStr = string.Join(" ", chunkWords);
            
            int chunkChars = string.Join("", chunkWords).Length;
            double chunkDuration = chunkChars * timePerChar;

            TimeSpan start = TimeSpan.FromSeconds(currentTime);
            TimeSpan end = TimeSpan.FromSeconds(currentTime + chunkDuration);

            srtBuilder.AppendLine($"{start:hh\\:mm\\:ss\\.fff} --> {end:hh\\:mm\\:ss\\.fff}");
            srtBuilder.AppendLine(chunkStr);
            srtBuilder.AppendLine();

            currentTime += chunkDuration;
        }

        File.WriteAllText(vttPath, srtBuilder.ToString());
        return vttPath;
    }
}
