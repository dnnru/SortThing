// based on https://github.com/ellman12/Photos-Storage-Server/blob/main/PSS/PSS/Backend/Metadata.cs

#region

using System;
using System.IO;
using System.Linq;
using SortThing.Contracts;
using SortThing.Models;

#endregion

namespace SortThing.Services
{
    public class FilenameTimestampReader : IFilenameTimestampReader
    {
        public Result<DateTime> GetFilenameTimestamp(string filePath)
        {
            DateTime dateTaken;
            try
            {
                dateTaken = File.GetLastWriteTime(filePath);
            }
            catch (Exception e)
            {
                return Result.Fail<DateTime>(e.Message);
            }

            var filename = Path.GetFileName(filePath);
            string timestamp = ""; //The actual timestamp in the filename, without the extra chars we don't want. Converted to DateTime at the end.

            try
            {
                //If Android screenshot. E.g., 'Screenshot_20201028-141626_Messages.jpg'
                if (filename.StartsWith("Screenshot_"))
                {
                    timestamp = string.Concat(filename.AsSpan(11, 8), filename.AsSpan(20, 6)); //Strip the chars we don't want.
                }
                else if (filename.StartsWith("IMG_") || filename.StartsWith("VID_"))
                {
                    timestamp = string.Concat(filename.AsSpan(4, 8), filename.AsSpan(13, 6));
                }
                else if (filename[4] == '-'
                      && filename[13] == '-'
                      && filename[16] == '-'
                      && filename.EndsWith(".mkv")) //Check if an OBS-generated file. It would have '-' at these 3 indices.
                {
                    timestamp = filename;
                    //Remove extension https://stackoverflow.com/questions/15564944/remove-the-last-three-characters-from-a-string
                    timestamp = filename[..(timestamp.Length - 4)];
                    timestamp = timestamp.Replace("-", "").Replace(" ", "");
                }
                //A filename like this: '20201031_090459.jpg'. I think these come from (Android(?)) phones. Not 100% sure.
                else if (filename[8] == '_' && !filename.StartsWith("messages"))
                {
                    timestamp = string.Concat(filename.AsSpan(0, 8), filename.AsSpan(9, 6));
                }
                //A Nintendo Switch screenshot/video clip, like '2018022016403700_s.mp4'.
                else if (filename.Contains("_s"))
                {
                    timestamp = filename[..14];
                }
                //Terraria's Capture Mode 'Capture 2020-05-16 21_04_54.png'
                else if (filename.StartsWith("Capture") && filename.EndsWith(".png"))
                {
                    timestamp = filename.Substring(8, 19);
                    timestamp = timestamp.Replace("-", "").Replace(":", "").Replace("_", "").Replace(" ", "");
                }
                //Not sure if these are exclusive to Terraria or what '20201226213009_1.jpg'
                else if (filename.EndsWith("_1.jpg"))
                {
                    timestamp = filename[..14];
                }
                //Might just be another Terraria-exclusive thing '105600_20201122143721_1.png'
                else if (filename.Contains("105600") && filename.EndsWith("_1.png"))
                {
                    timestamp = filename.Substring(7, 14);
                }
                //Stardew Valley uncompressed screenshots
                else if (filename.StartsWith("413150") && filename.EndsWith("_1.png"))
                {
                    timestamp = filename.Substring(7, 14);
                }
                //Snip & Sketch generates these filenames. E.g., 'Screenshot 2020-11-17 104051.png'
                else if (filename.StartsWith("Screenshot "))
                {
                    timestamp = filename.Substring(11, 17);
                    timestamp = timestamp.Replace("-", "").Replace(" ", "");
                }
            }
            catch
            {
                // Ignore
            }

            if (TryParseFilenameTimestampDateTime(timestamp, out DateTime dateTime) && dateTime != default)
            {
                dateTaken = dateTime;
            }

            return Result.Ok(dateTaken);
        }

        //Try parsing timestamp like this: "20211031155822"
        private static bool TryParseFilenameTimestampDateTime(string timestamp, out DateTime dateTaken)
        {
            if (string.IsNullOrWhiteSpace(timestamp) || timestamp.Length < 14 || !timestamp.All(char.IsDigit))
            {
                dateTaken = default;
                return false;
            }

            int year = int.Parse(timestamp[..4]);
            int month = int.Parse(timestamp[4..6]);
            int day = int.Parse(timestamp[6..8]);
            int hour = int.Parse(timestamp[8..10]);
            int min = int.Parse(timestamp[10..12]);
            int sec = int.Parse(timestamp[12..14]);

            try
            {
                dateTaken = new DateTime(year, month, day, hour, min, sec);
            }
            catch
            {
                dateTaken = default;
                return false;
            }

            return true;
        }
    }
}