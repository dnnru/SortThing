#region

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SortThing.Contracts;

#endregion

namespace SortThing.Services
{
    public class FileSystem : IFileSystem
    {
        public Task AppendAllLinesAsync(string path, IEnumerable<string> lines)
        {
            return File.AppendAllLinesAsync(path, lines);
        }

        public void CopyFile(string sourceFile, string destinationFile, bool overwrite)
        {
            File.Copy(sourceFile, destinationFile, overwrite);
        }

        public DirectoryInfo CreateDirectory(string directoryPath)
        {
            return Directory.CreateDirectory(directoryPath);
        }

        public Stream CreateFile(string filePath)
        {
            return File.Create(filePath);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public string[] GetFiles(string path, string searchPattern, EnumerationOptions enumOptions)
        {
            return Directory.GetFiles(path, searchPattern, enumOptions);
        }

        public void MoveFile(string sourceFile, string destinationFile, bool overwrite)
        {
            File.Move(sourceFile, destinationFile, overwrite);
        }

        public string ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public Task<string> ReadAllTextAsync(string path)
        {
            return File.ReadAllTextAsync(path);
        }

        public void WriteAllText(string filePath, string contents)
        {
            File.WriteAllText(filePath, contents);
        }

        public Task WriteAllTextAsync(string path, string content)
        {
            return File.WriteAllTextAsync(path, content);
        }
    }
}