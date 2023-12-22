#region

using System;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace SortThing.Utilities;

public static class Extensions
{
    private const char SINGLE_WILDCARD_CHARACTER = '?';
    private const char MULTI_WILDCARD_CHARACTER = '*';

    private static readonly char[] WildcardCharacters = { SINGLE_WILDCARD_CHARACTER, MULTI_WILDCARD_CHARACTER };

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
        string dirName = Path.GetInvalidPathChars()
            .Aggregate(Path.GetDirectoryName(name), (current, dir) => current.Replace(dir, invalidCharsReplacement))
            .Replace(new string(Enumerable.Repeat(Path.DirectorySeparatorChar, 2).ToArray()), Path.DirectorySeparatorChar.ToString());

        string fileName = Path.GetInvalidFileNameChars().Aggregate(Path.GetFileName(name), (current, c) => current.Replace(c, invalidCharsReplacement));
         
        return Path.Combine(dirName, fileName);
    }

    public static bool IsWildCardMatch(this string str, string pattern, StringComparison stringComparison = StringComparison.Ordinal)
    {
        // Pattern must contain something
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentOutOfRangeException(nameof(pattern));
        }

        // Uninitialised string never matches
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        // Multi character wildcard matches everything
        if (pattern == "*")
        {
            return true;
        }

        // Empty string does not match
        if (str.Length == 0)
        {
            return false;
        }

        var strSpan = str.AsSpan();
        var patternSpan = pattern.AsSpan();
        var strIndex = 0;

        for (var patternIndex = 0; patternIndex < pattern.Length; patternIndex++)
        {
            var patternCh = pattern[patternIndex];
            var patternChSpan = patternSpan.Slice(patternIndex, 1);

            if (strIndex == str.Length)
            {
                // At end of pattern for this longer string so always matches '*'
                return patternCh == '*' && patternIndex == pattern.Length - 1;
            }

            // Character match
            var strCh = strSpan.Slice(strIndex, 1);
            if (patternChSpan.Equals(strCh, stringComparison))
            {
                strIndex++;
                continue;
            }

            // Single wildcard match
            if (patternCh == '?')
            {
                strIndex++;
                continue;
            }

            // No match
            if (patternCh != '*')
            {
                return false;
            }

            // Multi character wildcard - last character in the pattern
            if (patternIndex == pattern.Length - 1)
            {
                return true;
            }

            // Match pattern to input string character-by-character until the next wildcard (or end of string if there is none)
            var patternChMatchStartIndex = patternIndex + 1;

            var nextWildcardIndex = pattern.IndexOfAny(WildcardCharacters, patternChMatchStartIndex);
            var patternChMatchEndIndex = nextWildcardIndex == -1 ? pattern.Length - 1 : nextWildcardIndex - 1;

            var comparisonLength = patternChMatchEndIndex - patternIndex;

            var comparison = patternSpan.Slice(patternChMatchStartIndex, comparisonLength);
            var skipToStringIndex = strSpan[strIndex..].IndexOf(comparison, stringComparison) + strIndex;

            // Handle repeated instances of the same character at end of pattern
            if (comparisonLength == 1 && nextWildcardIndex == -1)
            {
                var skipCandidateIndex = 0;
                while (skipCandidateIndex == 0)
                {
                    var skipToStringIndexNew = skipToStringIndex + 1;

                    skipCandidateIndex = strSpan[skipToStringIndexNew..].IndexOf(comparison, stringComparison);

                    if (skipCandidateIndex == 0)
                    {
                        skipToStringIndex = skipToStringIndexNew;
                    }
                }
            }

            if (skipToStringIndex == -1)
            {
                return false;
            }

            strIndex = skipToStringIndex;
        }

        // Pattern processing completed but rest of input string was not
        return strIndex >= str.Length;
    }
}