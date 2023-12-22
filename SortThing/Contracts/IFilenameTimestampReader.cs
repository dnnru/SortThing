#region

using System;
using SortThing.Models;

#endregion

namespace SortThing.Contracts;

public interface IFilenameTimestampReader
{
    Result<DateTime> GetFilenameTimestamp(string filePath);
}