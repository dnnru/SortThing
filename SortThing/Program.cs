#region

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using SortThing.Contracts;
using SortThing.Services;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

#endregion

namespace SortThing
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                        .MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
#else
                        .MinimumLevel.Warning()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
#endif
                        .Enrich.FromLogContext()
                        .Enrich.WithExceptionDetails()
                        .WriteTo.Async(c => c.File("Logs/LogFile_.log", rollingInterval: RollingInterval.Day, shared: true))
#if DEBUG
                        .WriteTo.Async(c => c.Console())
#endif
                        .CreateLogger();

            var rootCommand = new RootCommand("Sort your photos into folders based on metadata.");

            var configOption = new Option<string>(new[] { "--config-path", "-c" },
                                                  "The full path to the SortThing configuration file.  Use -g to generate a sample config file in the current directory.");

            configOption.AddValidator(option =>
                                      {
                                          if (!File.Exists(option.GetValueOrDefault()?.ToString()))
                                          {
                                              option.ErrorMessage = "Config file could not be found at the given path.";
                                          }
                                      });
            rootCommand.AddOption(configOption);

            var jobOption = new Option<string>(new[] { "--job-name", "-j" }, () => string.Empty, "If specified, will only run the named job from the config, then exit.");
            rootCommand.AddOption(jobOption);

            var watchOption = new Option<bool>(new[] { "--watch", "-w" },
                                               () => false,
                                               "If false, will run sort jobs immediately, then exit.  If true, will run jobs, then block and monitor for changes in each job's source folder.");
            rootCommand.AddOption(watchOption);

            var dryRunOption = new Option<bool>(new[] { "--dry-run", "-d" }, () => false, "If true, no file operations will actually be executed.");
            rootCommand.AddOption(dryRunOption);

            var generateOption = new Option<bool>(new[] { "--generate-config", "-g" }, () => false, "Generates a sample config file in the current directory.");
            rootCommand.AddOption(generateOption);

            rootCommand.SetHandler(async (string configPath, string jobName, bool watch, bool dryRun, bool generateSample) =>
                                   {
                                       using var host = Host.CreateDefaultBuilder(args)
                                                            .UseWindowsService(options => { options.ServiceName = "SortThing"; })
                                                            .UseConsoleLifetime()
                                                            .ConfigureServices(services =>
                                                                               {
                                                                                   services.AddScoped<IMetadataReader, MetadataReader>();
                                                                                   services.AddScoped<IFilenameTimestampReader, FilenameTimestampReader>();
                                                                                   services.AddScoped<IJobRunner, JobRunner>();
                                                                                   services.AddSingleton<IJobWatcher, JobWatcher>();
                                                                                   services.AddScoped<IPathTransformer, PathTransformer>();
                                                                                   services.AddScoped<IFileSystem, FileSystem>();
                                                                                   services.AddScoped<IReportWriter, ReportWriter>();
                                                                                   services.AddScoped<IConfigService, ConfigService>();
                                                                                   services.AddSingleton<ISystemTime, SystemTime>();
                                                                                   services.AddSingleton<IGlobalState>(new GlobalState()
                                                                                   {
                                                                                       ConfigPath = configPath,
                                                                                       DryRun = dryRun,
                                                                                       JobName = jobName,
                                                                                       Watch = watch,
                                                                                       GenerateSample = generateSample
                                                                                   });
                                                                                   services.AddHostedService<SortBackgroundService>();
                                                                               })
                                                            .ConfigureLogging(builder =>
                                                                              {
                                                                                  builder.ClearProviders();
                                                                                  builder.AddSerilog(dispose: true);
                                                                              })
                                                            .Build();

                                       await host.RunAsync().ConfigureAwait(false);
                                   },
                                   configOption,
                                   jobOption,
                                   watchOption,
                                   dryRunOption,
                                   generateOption);

            return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }
    }
}