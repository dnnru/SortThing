#region

using System;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace SortThing.Utilities
{
    public static class Extensions
    {
        public static T Apply<T>(this T self, Action<T> action)
        {
            action(self);
            return self;
        }

        public static string ReplaceEx(this string str, string oldValue, string newValue, StringComparison comparisonType)
        {
            if (str == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(oldValue))
            {
                return str;
            }

            StringBuilder result = new StringBuilder(Math.Min(4096, str.Length));
            int pos = 0;

            while (true)
            {
                int i = str.IndexOf(oldValue, pos, comparisonType);
                if (i < 0)
                {
                    break;
                }

                result.Append(str, pos, i - pos);
                result.Append(newValue);

                pos = i + oldValue.Length;
            }

            result.Append(str, pos, str.Length - pos);

            return result.ToString();
        }

        public static string ToValidFileName(this string name, char invalidCharsReplacement = '_')
        {
            return Path.GetInvalidFileNameChars()
                       .Aggregate(name, (current, c) => current.Replace(c, invalidCharsReplacement))
                       .Replace(new string(Enumerable.Repeat(Path.DirectorySeparatorChar, 2).ToArray()), Path.DirectorySeparatorChar.ToString());
        }
    }
}