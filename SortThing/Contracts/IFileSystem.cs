#region

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

#endregion

namespace SortThing.Contracts
{
    public interface IFileSystem
    {
        Task AppendAllLinesAsync(string path, IEnumerable<string> lines);
        void CopyFile(string sourceFile, string destinationFile, bool overwrite);
        DirectoryInfo CreateDirectory(string directoryPath);

        Stream CreateFile(string filePath);
        bool FileExists(string path);
        string[] GetFiles(string path, string searchPattern, EnumerationOptions enumOptions);

        void MoveFile(string sourceFile, string destinationFile, bool overwrite);
        string ReadAllText(string filePath);
        Task<string> ReadAllTextAsync(string path);
        void WriteAllText(string filePath, string contents);
        Task WriteAllTextAsync(string path, string content);
    }
}