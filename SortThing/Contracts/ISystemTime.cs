#region

using System;

#endregion

namespace SortThing.Contracts;

public interface ISystemTime
{
    DateTimeOffset Now { get; }
}