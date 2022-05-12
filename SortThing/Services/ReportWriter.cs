#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SortThing.Contracts;
using SortThing.Models;

#endregion

namespace SortThing.Services
{
    public class ReportWriter : IReportWriter
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<ReportWriter> _logger;
        private readonly ISystemTime _systemTime;

        public ReportWriter(ISystemTime systemTime, IFileSystem fileSystem, ILogger<ReportWriter> logger)
        {
            _systemTime = systemTime;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task<string> WriteReport(JobReport report)
        {
            var logPath = GetLogPath();
            await WriteReportInternal(report, logPath).ConfigureAwait(false);
            return logPath;
        }

        public async Task<string> WriteReports(IEnumerable<JobReport> reports)
        {
            var logPath = GetLogPath();
            foreach (var report in reports)
            {
                await WriteReportInternal(report, logPath).ConfigureAwait(false);
            }

            return logPath;
        }

        private string GetLogPath()
        {
            var logPath = Path.Combine(Path.GetTempPath(), "SortThing", $"JobReport_{_systemTime.Now:yyyy-MM-dd HH.mm.ss.fff}.log");
            _fileSystem.CreateDirectory(Path.GetDirectoryName(logPath));
            return logPath;
        }

        private async Task WriteReportInternal(JobReport report, string logPath)
        {
            try
            {
                _fileSystem.CreateDirectory(Path.GetDirectoryName(logPath) ?? "");

                var errors = new List<OperationResult>();
                var wasSkipped = new List<OperationResult>();
                var noExif = new List<OperationResult>();
                var successes = new List<OperationResult>();

                foreach (var result in report.Results)
                {
                    if (result.HadError)
                    {
                        errors.Add(result);
                    }

                    if (result.WasSkipped)
                    {
                        wasSkipped.Add(result);
                    }

                    if (!result.FoundExifData)
                    {
                        noExif.Add(result);
                    }

                    if (result.IsSuccess)
                    {
                        successes.Add(result);
                    }
                }

                var reportLines = new List<string>
                                  {
                                      $"Job Name: {report.JobName}",
                                      $"Operation: {report.Operation}",
                                      $"Dry Run: {report.DryRun}",
                                      $"Total Files: {report.Results.Count}",
                                      $"Successes: {successes.Count}",
                                      $"Errors: {errors.Count}",
                                      $"Skipped: {wasSkipped.Count}",
                                      $"No Exif: {noExif.Count}"
                                  };

                reportLines.AddRange(new[]
                                     {
                                         "",
                                         "#### Error Files ####",
                                         ""
                                     });
                foreach (var result in errors)
                {
                    reportLines.Add($"Pre-Operation Path: {result.PreOperationPath}\t" + $"Post-Operation Path: {result.PostOperationPath}\t");
                }

                reportLines.AddRange(new[]
                                     {
                                         "",
                                         "#### Skipped Files ####",
                                         ""
                                     });
                foreach (var result in wasSkipped)
                {
                    reportLines.Add($"Pre-Operation Path: {result.PreOperationPath}\t" + $"Post-Operation Path: {result.PostOperationPath}\t");
                }

                reportLines.AddRange(new[]
                                     {
                                         "",
                                         "#### No EXIF Files ####",
                                         ""
                                     });
                foreach (var result in noExif)
                {
                    reportLines.Add($"Pre-Operation Path: {result.PreOperationPath}\t" + $"Post-Operation Path: {result.PostOperationPath}\t");
                }

                reportLines.AddRange(new[]
                                     {
                                         "",
                                         "#### Success Files ####",
                                         ""
                                     });
                foreach (var result in successes)
                {
                    reportLines.Add($"Pre-Operation Path: {result.PreOperationPath}\t" + $"Post-Operation Path: {result.PostOperationPath}\t");
                }

                await _fileSystem.AppendAllLinesAsync(logPath, reportLines).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing SortThing report");
            }
        }
    }
}