using System.IO;

namespace CreateSnapshot_1
{
    internal static class FileSystem
    {
        public static void CopyDirectory(string sourcePath, string destinationPath)
        {
            Directory.CreateDirectory(destinationPath);
            CopyFiles(sourcePath, destinationPath);
            CopySubdirectories(sourcePath, destinationPath);
        }

        public static void CopyFiles(string sourcePath, string destinationPath)
        {
            foreach (string sourceFilePath in Directory.GetFiles(sourcePath))
            {
                var fileName = Path.GetFileName(sourceFilePath);
                var destinationFilePath = Path.Combine(destinationPath, fileName);
                File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
            }
        }

        public static void CopySubdirectories(string sourcePath, string destinationPath)
        {
            foreach (string sourceDirectoryPath in Directory.GetDirectories(sourcePath))
            {
                var directoryName = Path.GetFileName(sourceDirectoryPath);
                var destinationDirectoryPath = Path.Combine(destinationPath, directoryName);
                CopyDirectory(sourceDirectoryPath, destinationDirectoryPath);
            }
        }
    }
}
