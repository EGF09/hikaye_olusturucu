using System.Collections.Generic;
using System.Threading.Tasks;
using hikaye_olusturucu.Core.Models;

namespace hikaye_olusturucu.Core.Interfaces;

public interface ILLMService
{
    Task<string> GenerateStoryAsync(string prompt);
}

public interface IImageGenerationService
{
    Task<List<string>> GenerateImagesAsync(string storyContent, int count);
}

public interface ITtsService
{
    Task<string> GenerateAudioAsync(string text);
}

public interface IVideoService
{
    Task<string> CreateVideoAsync(List<string> imagePaths, string audioPath, string storyContent);
}

public interface IDatabaseService
{
    Task InitializeDatabaseAsync();
    Task SaveStoryAsync(Story story);
}