using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using hikaye_olusturucu.Core.Interfaces;
using hikaye_olusturucu.Services;
using hikaye_olusturucu.DataAccess;

namespace hikaye_olusturucu;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            
        IConfiguration config = builder.Build();

        var services = new ServiceCollection();
        
        string connStr = config["Database:ConnectionString"] ?? "Data Source=stories.db;Version=3;";
        string ffmpegPath = config["FFmpeg:ExecutablePath"] ?? "ffmpeg";

        string geminiKey = config["ApiKeys:Gemini"] ?? "";
        string hfKey = config["ApiKeys:HuggingFace"] ?? "";
        string pollinationsKey = config["ApiKeys:Pollinations"] ?? "";

        var freeApiService = new FreeApiService(geminiKey, hfKey, pollinationsKey);

        services.AddSingleton<ILLMService>(freeApiService);
        services.AddSingleton<IImageGenerationService>(freeApiService);
        services.AddSingleton<ITtsService>(new GoogleTtsService(ffmpegPath));
        services.AddSingleton<IVideoService>(new FFmpegVideoService(ffmpegPath));
        services.AddSingleton<IDatabaseService>(new SqliteDatabaseService(connStr));
        
        services.AddTransient<Form1>();

        using var serviceProvider = services.BuildServiceProvider();
        var form1 = serviceProvider.GetRequiredService<Form1>();
        
        Application.Run(form1);
    }
}