#region

using System;
using SortThing.Models;

#endregion

namespace SortThing.Contracts;

public interface IMetadataReader
{
    Result<DateTime> ParseExifDateTime(string exifDateTime);
    Result<ExifData> TryGetExifData(string filePath);
}