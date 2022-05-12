namespace SortThing.Contracts
{
    public interface IGlobalState
    {
        string ConfigPath { get; init; }
        bool DryRun { get; init; }
        bool GenerateSample { get; init; }
        string JobName { get; init; }
        bool Watch { get; init; }
    }
}