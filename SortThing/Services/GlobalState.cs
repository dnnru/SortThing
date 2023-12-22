using SortThing.Contracts;

namespace SortThing.Services;

public class GlobalState : IGlobalState
{
    public string ConfigPath { get; init; } = string.Empty;
    public bool DryRun { get; init; }
    public bool GenerateSample { get; init; }
    public string JobName { get; init; } = string.Empty;
    public bool Watch { get; init; }
}