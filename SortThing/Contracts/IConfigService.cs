#region

using System.Threading.Tasks;
using SortThing.Models;

#endregion

namespace SortThing.Contracts;

public interface IConfigService
{
    Task<SortConfig> GetConfig(string configPath);
    Task<SortConfig> GetSortConfig();
    Task<Result<string>> TryFindConfig();
    Task GenerateSample();
}