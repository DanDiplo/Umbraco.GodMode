using System.IO;
using System.Threading.Tasks;

namespace Diplo.GodMode.Helpers
{
    internal static class IOHelper
    {
        /// <summary>
        /// Attempts to delete a directory and, if it fails, retries after a period for a defined number of retry attempts
        /// </summary>
        /// <param name="directory">The full path to delete</param>
        /// <param name="retries">The max number of retries to attempt</param>
        /// <param name="sleepMs">The time in milliseconds to pause between retries</param>
        /// <returns>True if succeeded; otherwise false</returns>
        public async static Task<bool> DeleteDirectoryRecursivelyWithRetriesAsync(string directory, int retries = 10, int sleepMs = 100)
        {
            if (!Directory.Exists(directory))
            {
                return true;
            }

            for (var retry = 1; retry <= retries; retry++)
            {
                try
                {
                    Directory.Delete(directory, true);
                    return true;
                }
                catch (DirectoryNotFoundException)
                {
                    return true;  // good!
                }
                catch
                {
                    await Task.Delay(sleepMs);
                    continue;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to delete a file and, if it fails, retries after a period for a defined number of retry attempts
        /// </summary>
        /// <param name="filePath">The full path of the file to delete</param>
        /// <param name="retries">The max number of retries to attempt</param>
        /// <param name="sleepMs">The time in milliseconds to pause between retries</param>
        /// <returns>True if succeeded; otherwise false</returns>
        public async static Task<bool> DeleteFileWithRetriesAsync(string filePath, int retries = 5, int sleepMs = 100)
        {
            if (!File.Exists(filePath))
            {
                return true;
            }

            for (var retry = 1; retry <= retries; retry++)
            {
                try
                {
                    File.Delete(filePath);
                    return true;
                }
                catch (FileNotFoundException)
                {
                    return true;  // good!
                }
                catch
                {
                    await Task.Delay(sleepMs);
                    continue;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to write the specified file contents to the file path and, if it fails, retries after a period for a defined number of retry attempts. If fails after that, throws an IO exception.
        /// </summary>
        /// <param name="filePath">The full file path</param>
        /// <param name="content">The content to write</param>
        /// <param name="retries">The max number of retries to attempt</param>
        /// <param name="sleepMs">The time in milliseconds to pause between retries</param>
        public static async Task WriteFileWithRetriesAsync(string filePath, string content, int retries = 5, int sleepMs = 100)
        {
            for (var retry = 1; retry <= retries; retry++)
            {
                try
                {
                    await File.WriteAllTextAsync(filePath, content);
                    return;
                }
                catch
                {
                    if (retry == retries)
                    {
                        throw;
                    }

                    await Task.Delay(sleepMs);

                    continue;
                }
            }
        }
    }
}
