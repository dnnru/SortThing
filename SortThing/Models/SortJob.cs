#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using MimeMapping;
using SortThing.Enums;
using SortThing.Utilities;

#endregion

namespace SortThing.Models;

public class SortJob
{
    public string DestinationFile { get; init; } = string.Empty;
    public string DestinationNoExifFile { get; init; } = string.Empty;
    public string NoExifDirectory { get; init; } = string.Empty;

    public string[] ExcludeExtensions { get; init; } = Array.Empty<string>();

    [JsonIgnore]
    public string[] ExcludeExtensionsExpanded => GetExtensionsByMimeType(ExcludeExtensions);

    public string[] IncludeExtensions { get; init; } = Array.Empty<string>();

    [JsonIgnore]
    public string[] IncludeExtensionsExpanded => GetExtensionsByMimeType(IncludeExtensions);

    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     The operation to perform on the original files.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SortOperation Operation { get; init; }

    /// <summary>
    ///     The action to take when destination file already exists.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OverwriteAction OverwriteAction { get; init; }

    public string SourceDirectory { get; init; } = string.Empty;

    public bool UseTimestamp { get; init; }

    private string[] GetExtensionsByMimeType(string[] extensions)
    {
            var result = new List<string>(extensions);
            var mimetypes = extensions.Where(e => e.Trim().IndexOf("mimetype", 0, StringComparison.InvariantCultureIgnoreCase) >= 0).ToList();
            result.RemoveAll(str => mimetypes.Contains(str));

            foreach (var mimeTypePatterns in mimetypes.Select(mimetype => mimetype.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                                      .Where(mimetypePatterns => mimetypePatterns.Length >= 2))
            {
                result.AddRange(MimeUtility.TypeMap.Where(kvp => kvp.Value.IsWildCardMatch(mimeTypePatterns[1], StringComparison.InvariantCultureIgnoreCase))
                                           .Select(kvp => kvp.Key));
            }

            for (int i = 0; i < result.Count; i++)
            {
                result[i] = result[i].Trim().TrimStart('.').ToLower();
            }

            return result.Distinct().ToArray();
        }
}