#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Avi;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using SortThing.Contracts;
using SortThing.Models;
using SortThing.Utilities;
using Directory = MetadataExtractor.Directory;

#endregion

namespace SortThing.Services
{
    public class MetadataReader : IMetadataReader
    {
        private readonly ConcurrentDictionary<string, IReadOnlyList<Directory>> _directoriesDictionary = new ConcurrentDictionary<string, IReadOnlyList<Directory>>();

        /// <summary>
        ///     Formats an EXIF DateTime to a format that can be parsed in .NET.
        /// </summary>
        /// <param name="exifDateTime"></param>
        /// <returns></returns>
        public Result<DateTime> ParseExifDateTime(string exifDateTime)
        {
            if (string.IsNullOrWhiteSpace(exifDateTime))
            {
                return Result.Fail<DateTime>($"Parameter {nameof(exifDateTime)} cannot be empty.");
            }

            if (exifDateTime.Count(character => character == ':') < 2)
            {
                return Result.Fail<DateTime>($"Parameter {nameof(exifDateTime)} appears to be invalid.");
            }

            var dateArray = exifDateTime.Split(" ", StringSplitOptions.RemoveEmptyEntries).Apply(split => split[0] = split[0].Replace(':', '-'));

            return !DateTime.TryParse(string.Join(' ', dateArray), out var dateTaken) ? Result.Fail<DateTime>("Unable to parse DateTime metadata value.") : Result.Ok(dateTaken);
        }

        public Result<ExifData> TryGetExifData(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return Result.Fail<ExifData>("File could not be found.");
                }

                if (!TryGetDateTime(filePath, out var dateTaken))
                {
                    return Result.Fail<ExifData>("DateTime is missing from metadata.");
                }

                TryGetCameraModel(filePath, out var camera);
                TryGetLocation(filePath, out var location);

                return Result.Ok(new ExifData
                                 {
                                     DateTaken = dateTaken,
                                     CameraModel = camera?.Trim(),
                                     Location = location
                                 });
            }
            catch
            {
                return Result.Fail<ExifData>("Error while reading metadata.");
            }
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool TryGetLocation(string filePath, out (double Latitude, double Longitude)? location)
        {
            location = null;
            if (!TryGetExifDirectory<GpsDirectory>(filePath, out var gpsDirectory))
            {
                return false;
            }

            var geoLocation = gpsDirectory?.GetGeoLocation();

            if (geoLocation == null)
            {
                return false;
            }

            location = (geoLocation.Latitude, geoLocation.Longitude);
            return true;
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool TryGetCameraModel(string filePath, out string camera)
        {
            camera = string.Empty;

            if (!TryGetExifDirectory<ExifIfd0Directory>(filePath, out var directory))
            {
                if (!TryGetExifDirectory<QuickTimeMetadataHeaderDirectory>(filePath, out var qtDir))
                {
                    return false;
                }

                camera = qtDir?.GetString(QuickTimeMetadataHeaderDirectory.TagModel)?.Trim();
                return !string.IsNullOrWhiteSpace(camera);
            }

            camera = directory?.GetString(ExifDirectoryBase.TagModel)?.Trim();

            return !string.IsNullOrWhiteSpace(camera);
        }

        private bool TryGetDateTime(string filePath, out DateTime dateTaken)
        {
            dateTaken = default;

            if (!TryGetExifDirectory<ExifSubIfdDirectory>(filePath, out var directory))
            {
                if (TryGetExifDirectory<QuickTimeMovieHeaderDirectory>(filePath, out var movieDir))
                {
                    return movieDir.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out dateTaken);
                }

                if (TryGetExifDirectory<QuickTimeMetadataHeaderDirectory>(filePath, out var metadataDir))
                {
                    return metadataDir.TryGetDateTime(QuickTimeMetadataHeaderDirectory.TagCreationDate, out dateTaken);
                }

                return TryGetExifDirectory<AviDirectory>(filePath, out var aviDir) && aviDir.TryGetDateTime(AviDirectory.TagDateTimeOriginal, out dateTaken);
            }

            return directory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out dateTaken)
                || directory.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out dateTaken)
                || directory.TryGetDateTime(ExifDirectoryBase.TagDateTime, out dateTaken);
        }

        private bool TryGetExifDirectory<T>(string filePath, out T directory) where T : class
        {
            directory = _directoriesDictionary.GetOrAdd(filePath, ReadMetadata)?.OfType<T>().FirstOrDefault();
            return directory is not null;
        }

        private static IReadOnlyList<Directory> ReadMetadata(string filePath)
        {
            IReadOnlyList<Directory> directories = null;
            try
            {
                directories = ImageMetadataReader.ReadMetadata(filePath);
            }
            catch
            {
                // Ignore
            }

            return directories;
        }
    }
}