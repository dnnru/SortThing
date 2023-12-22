#region

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SortThing.Contracts;

public interface IJobWatcher
{
    Task CancelWatchers();
    Task WatchJobs(string configPath, bool dryRun, CancellationToken cancelToken);
}