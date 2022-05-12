#region

using System;

#endregion

namespace SortThing.Contracts
{
    public interface IPathTransformer
    {
        string TransformPath(string sourcePath, string destinationPath);
        string TransformPath(string sourcePath, string destinationPath, DateTime dateTaken, string camera);
        string GetUniqueFilePath(string destinationFile);
        string TransformPath(string sourcePath, string destinationPath, DateTime fileCreated);
    }
}