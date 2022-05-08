using Microsoft.Extensions.Logging;
using SortThing.Abstractions;
using SortThing.Enums;
using SortThing.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IConfigService
    {
        Task<SortConfig> GetConfig(string configPath);
        Task<SortConfig> GetSortConfig();
        Task<Result<string>> TryFindConfig();
        Task GenerateSample();
    }

    public class ConfigService : IConfigService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<ConfigService> _logger;

        public ConfigService(IFileSystem fileSystem, ILogger<ConfigService> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        private string DefaultConfigPath
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SortThing", "Config.json");
                }
                else
                {
                    return Path.Combine(Path.GetTempPath(), "SortThing", "Config.json");
                }
            }
        }

        private JsonSerializerOptions JsonSerializerOptions => new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            WriteIndented = true
        };

        public async Task GenerateSample()
        {
            var config = new SortConfig()
            {
                Jobs = new[]
                {
                    new SortJob()
                    {
                        Name = "Photos",
                        Operation = SortOperation.Move,
                        SourceDirectory = "D:\\Sync\\Camera\\",
                        DestinationFile = $"D:\\Sorted\\Photos\\{PathTransformer.Year}\\{PathTransformer.Month}\\{PathTransformer.Day}\\{PathTransformer.Camera}\\{PathTransformer.Hour}{PathTransformer.Minute} - {PathTransformer.Filename}.{PathTransformer.Extension}",
                        NoExifDirectory = "D:\\Sorted\\NoExif\\Photos",
                        IncludeExtensions = new[] { "png", "jpg", "jpeg" },
                        ExcludeExtensions = Array.Empty<string>(),
                        OverwriteAction =  OverwriteAction.Overwrite,
                        UseTimestamp = false
                    },

                    new SortJob()
                    {
                        Name = "Videos",
                        Operation = SortOperation.Move,
                        SourceDirectory = "D:\\Sync\\Camera\\",
                        DestinationFile = $"D:\\Sorted\\Videos\\{PathTransformer.Year}\\{PathTransformer.Month}\\{PathTransformer.Day}\\{PathTransformer.Camera}\\{PathTransformer.Hour}{PathTransformer.Minute} - {PathTransformer.Filename}.{PathTransformer.Extension}",
                        NoExifDirectory = "D:\\Sorted\\NoExif\\Videos",
                        IncludeExtensions = new[] { "mp4", "avi", "m4v", "mov" },
                        ExcludeExtensions = Array.Empty<string>(),
                        OverwriteAction = OverwriteAction.New,
                        UseTimestamp = false
                    },

                    new SortJob()
                    {
                        Name = "Others",
                        Operation = SortOperation.Move,
                        SourceDirectory = "D:\\Sync\\Camera\\",
                        DestinationFile = $"D:\\Sorted\\Files\\{PathTransformer.Year}\\{PathTransformer.Month}\\{PathTransformer.Day}\\{PathTransformer.Hour}{PathTransformer.Minute} - {PathTransformer.Filename}.{PathTransformer.Extension}",
                        IncludeExtensions = new[] { "*" },
                        ExcludeExtensions = new[] { "png", "jpg", "jpeg", "mp4", "avi", "m4v", "mov" },
                        OverwriteAction = OverwriteAction.Skip,
                        UseTimestamp = false
                    }
                }
            };

            var serialized = JsonSerializer.Serialize(config, JsonSerializerOptions);

            try
            {
                await _fileSystem.WriteAllTextAsync("ExampleConfig.json", serialized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write example config to disk.");
            }
        }

        public async Task<SortConfig> GetConfig(string configPath)
        {

            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentNullException(nameof(configPath));
            }

            if (!_fileSystem.FileExists(configPath))
            {
                return new();
            }

            var configString = await _fileSystem.ReadAllTextAsync(configPath);
            return JsonSerializer.Deserialize<SortConfig>(configString, JsonSerializerOptions) ?? new();
        }

        public async Task<SortConfig> GetSortConfig()
        {
            try
            {
                return await GetConfig(DefaultConfigPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sort config.");
            }
            return new SortConfig();
        }

        public async Task<Result<string>> TryFindConfig()
        {
            var exeDir = Path.GetDirectoryName(Environment.CommandLine.Split(" ").First());
            var directory = _fileSystem.CreateDirectory(exeDir);

            var jsonFiles = directory.GetFiles("*.json");

            foreach (var file in jsonFiles)
            {
                try
                {
                    var content = await _fileSystem.ReadAllTextAsync(file.FullName);
                    var config = JsonSerializer.Deserialize<SortConfig>(content, JsonSerializerOptions);
                    if (config is not null)
                    {
                        _logger.LogInformation("Found config file: {configPath}.", file.FullName);
                        return Result.Ok(file.FullName);
                    }
                }
                catch { }
            }

            _logger.LogWarning("No config file was found in {exeDir}.", exeDir);
            return Result.Fail<string>($"No config file was found in {exeDir}.");
        }
    }
}
