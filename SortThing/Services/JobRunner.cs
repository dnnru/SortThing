#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ShellProgressBar;
using SortThing.Contracts;
using SortThing.Enums;
using SortThing.Models;
using SortThing.Utilities;

#endregion

namespace SortThing.Services;

public class JobRunner : IJobRunner
{
    private static readonly SemaphoreSlim RunLock = new SemaphoreSlim(1, 1);
    private readonly IConfigService _configService;

    private readonly EnumerationOptions _enumOptions = new EnumerationOptions
    {
        AttributesToSkip =
            FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System,
        RecurseSubdirectories = true,
        MatchCasing = MatchCasing.PlatformDefault
    };

    private readonly IFilenameTimestampReader _filenameTimestampReader;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<JobRunner> _logger;
    private readonly IMetadataReader _metaDataReader;
    private readonly IPathTransformer _pathTransformer;

    public JobRunner(IFileSystem fileSystem,
        IMetadataReader metaDataReader,
        IFilenameTimestampReader filenameTimestampReader,
        IPathTransformer pathTransformer,
        IConfigService configService,
        ILogger<JobRunner> logger)
    {
            _fileSystem = fileSystem;
            _metaDataReader = metaDataReader;
            _filenameTimestampReader = filenameTimestampReader;
            _pathTransformer = pathTransformer;
            _configService = configService;
            _logger = logger;
        }

    public async Task<JobReport> RunJob(SortJob job, bool dryRun, CancellationToken cancelToken)
    {
            var jobReport = new JobReport()
                            {
                                JobName = job.Name,
                                Operation = job.Operation,
                                DryRun = dryRun
                            };
            
            ProgressBar progressBar = null;

            try
            {
                await RunLock.WaitAsync(cancelToken).ConfigureAwait(false);

                _logger.LogInformation("Starting job run: {job}", JsonSerializer.Serialize(job));
                Console.WriteLine($"{Environment.NewLine}Starting job run: {job.Name}");
                
                var fileList = new List<string>();

                for (var extIndex = 0; extIndex < job.IncludeExtensionsExpanded.Length; extIndex++)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Job run cancelled");
                        Console.WriteLine("Job run cancelled");
                        break;
                    }

                    var extension = job.IncludeExtensionsExpanded[extIndex];

                    var files = _fileSystem.GetFiles(job.SourceDirectory, $"*.{extension.Replace(".", "")}", _enumOptions)
                                           .Where(file => !job.ExcludeExtensionsExpanded.Any(ext => ext.Equals(Path.GetExtension(file)[1..], StringComparison.OrdinalIgnoreCase)))
                                           .ToArray();

                    fileList.AddRange(files);
                }
                
                progressBar = ProgressBarHelper.CreateProgressBar(fileList.Count, $"Running job '{job.Name}'");
                IProgress<OperationResult> progress = progressBar.AsProgress<OperationResult>(e => $"Processed file [{Path.GetFileName(e.PreOperationPath)}]. Skipped: {e.WasSkipped}, had error: {e.HadError}");
                
                for (var fileIndex = 0; fileIndex < fileList.Count; fileIndex++)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Job run cancelled");
                        progressBar.WriteErrorLine("Job run cancelled");
                        break;
                    }

                    var file = fileList[fileIndex];

                    _logger.LogInformation("[{progress}%] Processing file {index} out of {total}", (float)fileIndex / fileList.Count * 100, fileIndex + 1, fileList.Count);

                    var result = await PerformFileOperation(job, dryRun, file).ConfigureAwait(false);
                    jobReport.Results.Add(result);
                    progress.Report(result);
                }
            }
            catch (Exception ex)
            {
                progressBar?.WriteErrorLine(ex.Message);
                _logger.LogError(ex, "Error while running job");
            }
            finally
            {
                progressBar?.Dispose();
                RunLock.Release();
            }

            return jobReport;
        }

    public async Task<JobReport> RunJob(string configPath, string jobName, bool dryRun, CancellationToken cancelToken)
    {
            var config = await _configService.GetConfig(configPath).ConfigureAwait(false);
            var job = config.Jobs?.FirstOrDefault(x => x.Name?.Equals(jobName, StringComparison.OrdinalIgnoreCase) ?? false);

            if (job is null)
            {
                _logger.LogError("Job name {jobName} not found in config", jobName);
                return new JobReport()
                       {
                           DryRun = dryRun,
                           JobName = jobName,
                           Operation = SortOperation.Unknown
                       };
            }

            return await RunJob(job, dryRun, cancelToken).ConfigureAwait(false);
        }

    public async Task<List<JobReport>> RunJobs(string configPath, bool dryRun, CancellationToken cancelToken)
    {
            var config = await _configService.GetConfig(configPath).ConfigureAwait(false);
            var reports = new List<JobReport>();

            foreach (var job in config.Jobs)
            {
                var report = await RunJob(job, dryRun, cancelToken).ConfigureAwait(false);
                reports.Add(report);
            }

            return reports;
        }

    private Task<OperationResult> PerformFileOperation(SortJob job, bool dryRun, string file)
    {
            OperationResult operationResult;
            var exifFound = false;
            var destinationFile = string.Empty;

            try
            {
                var result = _metaDataReader.TryGetExifData(file);
                var timestampResult = job.UseTimestamp ? _filenameTimestampReader.GetFilenameTimestamp(file) : null;

                if (result.IsSuccess && result.Value is not null)
                {
                    exifFound = true;
                    destinationFile = _pathTransformer.TransformPath(file, job.DestinationFile, result.Value.DateTaken, result.Value.CameraModel);
                }
                else if (timestampResult is { IsSuccess: true } && timestampResult.Value != DateTime.MinValue)
                {
                    destinationFile = _pathTransformer.TransformPath(file, job.DestinationNoExifFile, timestampResult.Value);
                }
                else
                {
                    var noExifPath = Path.Combine(job.NoExifDirectory, Path.GetFileName(file));
                    destinationFile = _pathTransformer.GetUniqueFilePath(noExifPath);
                }

                if (dryRun)
                {
                    _logger.LogInformation("Dry run - Skipping file operation Source: {file}.  Destination: {destinationFile}", file, destinationFile);

                    operationResult = new OperationResult()
                                      {
                                          FoundExifData = exifFound,
                                          PostOperationPath = destinationFile,
                                          WasSkipped = true,
                                          PreOperationPath = file
                                      };

                    return Task.FromResult(operationResult);
                }

                if (_fileSystem.FileExists(destinationFile) && job.OverwriteAction == OverwriteAction.Skip)
                {
                    _logger.LogWarning("Destination file exists.  Skipping.  Destination file: {destinationFile}", destinationFile);
                    operationResult = new OperationResult()
                                      {
                                          FoundExifData = exifFound,
                                          WasSkipped = true,
                                          PostOperationPath = destinationFile,
                                          PreOperationPath = file
                                      };
                    return Task.FromResult(operationResult);
                }

                if (_fileSystem.FileExists(destinationFile) && job.OverwriteAction == OverwriteAction.New)
                {
                    _logger.LogWarning("Destination file exists. Creating unique file name");
                    destinationFile = _pathTransformer.GetUniqueFilePath(destinationFile);
                }

                _logger.LogInformation("Starting file operation: {jobOperation} Source: {file}  Destination: {destinationFile}", job.Operation, file, destinationFile);

                var dirName = Path.GetDirectoryName(destinationFile);
                if (dirName is null)
                {
                    throw new DirectoryNotFoundException($"Unable to get directory name for file {destinationFile}.");
                }

                Directory.CreateDirectory(dirName);

                switch (job.Operation)
                {
                    case SortOperation.Move:
                        _fileSystem.MoveFile(file, destinationFile, true);
                        break;
                    case SortOperation.Copy:
                        _fileSystem.CopyFile(file, destinationFile, true);
                        break;
                    case SortOperation.Unknown:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                operationResult = new OperationResult()
                                  {
                                      FoundExifData = exifFound,
                                      PostOperationPath = destinationFile,
                                      PreOperationPath = file
                                  };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while running job.");

                operationResult = new OperationResult()
                                  {
                                      FoundExifData = exifFound,
                                      PostOperationPath = destinationFile,
                                      PreOperationPath = file,
                                      HadError = true
                                  };
            }

            return Task.FromResult(operationResult);
        }
}