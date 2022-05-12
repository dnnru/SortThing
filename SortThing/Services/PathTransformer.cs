#region

using System;
using System.IO;
using SortThing.Contracts;
using SortThing.Utilities;

#endregion

namespace SortThing.Services
{
    public class PathTransformer : IPathTransformer
    {
        public const string CAMERA = "{camera}";
        public const string DAY = "{day}";
        public const string EXTENSION = "{extension}";
        public const string FILENAME = "{filename}";
        public const string HOUR = "{hour}";
        public const string MINUTE = "{minute}";
        public const string MONTH = "{month}";
        public const string YEAR = "{year}";

        public string GetUniqueFilePath(string destinationFile)
        {
            var uniquePath = destinationFile;

            for (var i = 0;; i++)
            {
                if (!File.Exists(uniquePath))
                {
                    break;
                }

                var filename = Path.GetFileNameWithoutExtension(destinationFile) + $"_{i}" + Path.GetExtension(destinationFile);

                uniquePath = Path.Combine(Path.GetDirectoryName(destinationFile) ?? throw new InvalidOperationException(), filename);
            }

            return uniquePath;
        }

        public string TransformPath(string sourceFile, string destinationFile, DateTime dateTaken, string camera)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }

            if (string.IsNullOrWhiteSpace(destinationFile))
            {
                throw new ArgumentNullException(nameof(destinationFile));
            }

            destinationFile = string.IsNullOrWhiteSpace(camera)
                                  ? destinationFile.ReplaceEx(CAMERA, "", StringComparison.OrdinalIgnoreCase)
                                  : destinationFile.ReplaceEx(CAMERA, camera.Trim(), StringComparison.OrdinalIgnoreCase);

            return destinationFile.ReplaceEx(YEAR, dateTaken.Year.ToString().PadLeft(4, '0'), StringComparison.OrdinalIgnoreCase)
                                  .ReplaceEx(MONTH, dateTaken.Month.ToString().PadLeft(2, '0'), StringComparison.OrdinalIgnoreCase)
                                  .ReplaceEx(DAY, dateTaken.Day.ToString().PadLeft(2, '0'), StringComparison.OrdinalIgnoreCase)
                                  .ReplaceEx(HOUR, dateTaken.Hour.ToString().PadLeft(2, '0'), StringComparison.OrdinalIgnoreCase)
                                  .ReplaceEx(MINUTE, dateTaken.Minute.ToString().PadLeft(2, '0'), StringComparison.OrdinalIgnoreCase)
                                  .ReplaceEx(FILENAME, Path.GetFileNameWithoutExtension(sourceFile), StringComparison.OrdinalIgnoreCase)
                                  .ReplaceEx(EXTENSION, Path.GetExtension(sourceFile)[1..], StringComparison.OrdinalIgnoreCase)
                                  .ToValidFileName();
        }

        public string TransformPath(string sourceFile, string destinationFile)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }

            if (string.IsNullOrWhiteSpace(destinationFile))
            {
                throw new ArgumentNullException(nameof(destinationFile));
            }

            return destinationFile.Replace(FILENAME, Path.GetFileNameWithoutExtension(sourceFile)).Replace(EXTENSION, Path.GetExtension(sourceFile)[1..]);
        }

        public string TransformPath(string sourcePath, string destinationPath, DateTime fileCreated)
        {
            return TransformPath(sourcePath, destinationPath, fileCreated, string.Empty);
        }
    }
}