#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SortThing.Contracts;
using SortThing.Models;
using SortThing.Utilities;

#endregion

namespace SortThing.Services
{
    public class JobWatcher : IJobWatcher
    {
        private readonly ConcurrentDictionary<object, SemaphoreSlim> _jobRunLocks = new ConcurrentDictionary<object, SemaphoreSlim>();
        private readonly IJobRunner _jobRunner;
        private readonly ILogger<JobWatcher> _logger;
        private readonly IReportWriter _reportWriter;
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private readonly SemaphoreSlim _watchersLock = new SemaphoreSlim(1, 1);

        public JobWatcher(IJobRunner jobRunner, IReportWriter reportWriter, ILogger<JobWatcher> logger)
        {
            _jobRunner = jobRunner;
            _reportWriter = reportWriter;
            _logger = logger;
        }

        public Task CancelWatchers()
        {
            foreach (var watcher in _watchers)
            {
                try
                {
                    watcher.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while disposing of watcher");
                }
                finally
                {
                    _watchers.Remove(watcher);
                }
            }

            return Task.CompletedTask;
        }

        public async Task WatchJobs(string configPath, bool dryRun, CancellationToken cancelToken)
        {
            try
            {
                await _watchersLock.WaitAsync(cancelToken).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(configPath))
                {
                    throw new ArgumentNullException(nameof(configPath));
                }

                var configString = await File.ReadAllTextAsync(configPath, cancelToken).ConfigureAwait(false);
                var config = JsonSerializer.Deserialize<SortConfig>(configString);

                if (config is null)
                {
                    throw new SerializationException("Config file could not be deserialized.");
                }

                await CancelWatchers().ConfigureAwait(false);

                foreach (var job in config.Jobs)
                {
                    var key = Guid.NewGuid();
                    var watcher = new FileSystemWatcher(job.SourceDirectory)
                                  {
                                      IncludeSubdirectories = true
                                  };

                    foreach (var ext in job.IncludeExtensions)
                    {
                        watcher.Filters.Add($"*.{ext.Replace(".", "")}");
                    }

                    _watchers.Add(watcher);

                    watcher.Created += (sender, ev) => { _ = RunJob(key, job, dryRun, cancelToken); };

                    watcher.EnableRaisingEvents = true;
                }
            }
            finally
            {
                _watchersLock.Release();
            }
        }

        private async Task RunJob(Guid jobKey, SortJob job, bool dryRun, CancellationToken cancelToken)
        {
            var jobRunLock = _jobRunLocks.GetOrAdd(jobKey, _ => new SemaphoreSlim(1, 1));

            if (!await jobRunLock.WaitAsync(0, cancelToken).ConfigureAwait(false))
            {
                return;
            }

            async void Action()
            {
                try
                {
                    var report = await _jobRunner.RunJob(job, dryRun, cancelToken).ConfigureAwait(false);
                    await _reportWriter.WriteReport(report).ConfigureAwait(false);
                }
                finally
                {
                    _jobRunLocks.TryRemove(jobKey, out _);
                }
            }

            Debouncer.Debounce(jobKey, TimeSpan.FromSeconds(5), Action);
        }
    }
}