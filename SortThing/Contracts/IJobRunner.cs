#region

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SortThing.Models;

#endregion

namespace SortThing.Contracts;

public interface IJobRunner
{
    Task<JobReport> RunJob(SortJob job, bool dryRun, CancellationToken cancelToken);

    Task<JobReport> RunJob(string configPath, string jobName, bool dryRun, CancellationToken cancelToken);

    Task<List<JobReport>> RunJobs(string configPath, bool dryRun, CancellationToken cancelToken);
}