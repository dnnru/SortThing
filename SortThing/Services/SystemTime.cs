#region

using System;
using SortThing.Contracts;

#endregion

namespace SortThing.Services;

public class SystemTime : ISystemTime
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}