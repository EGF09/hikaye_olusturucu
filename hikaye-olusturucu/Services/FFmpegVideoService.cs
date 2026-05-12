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
        string subtitlePath = CreateSubtitleFile(storyContent, totalAudioDuration);
        string srtPath = subtitlePath.Replace("\\", "/").Replace(":", "\\:");

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

        // Altyazı ekleme
        sbFilters.Append($"[vmerged]subtitles='{srtPath}':force_style='FontSize=24,PrimaryColour=&H00FFFFFF,OutlineColour=&H00000000'[finalv]");

        // Ses dosyası en son girdi (index = imagePaths.Count)
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
