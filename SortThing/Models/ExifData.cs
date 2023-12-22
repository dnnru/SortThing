#region

using System;

#endregion

namespace SortThing.Models;

public class ExifData
{
    public DateTime DateTaken { get; init; }
    public string CameraModel { get; init; }
    public (double Latitude, double Longitude)? Location { get; init; }
}