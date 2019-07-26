using System.Collections.Generic;
using Umbraco.Core.Models;

namespace Diplo.GodMode.Helpers
{
    /// <summary>
    /// Helper for determing file type information
    /// There's something in Net that now does this, but it works, so...
    /// </summary>
    internal static class FileTypeHelper
    {
        /// <summary>
        /// Gets the friendly file type name for a given extension
        /// </summary>
        /// <param name="extension">The file extensions (without dot)</param>
        /// <returns>The mapped friendly name or 'Unknown'</returns>
        internal static string GetFileTypeName(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return "Not Set";
            }


            if (fileTypes.TryGetValue(extension, out string name))
            {
                return name;
            }

            return "Unknown";
        }

        /// <summary>
        /// Gets the file size from the umbracoBytes property
        /// </summary>
        /// <param name="m">The media</param>
        /// <returns>The size in bytes, or zero if not set</returns>
        internal static int GetFileSize(IMedia m)
        {
            if (m.HasProperty("umbracoBytes"))
            {
                return m.GetValue<int>("umbracoBytes");
            }

            return 0;
        }

        /// <summary>
        /// Gets the file extension name from the umbracoExtension property
        /// </summary>
        /// <param name="m">The media</param>
        /// <returns>The file extension (without leading dot)</returns>
        internal static string GetExtensionFromMedia(IMedia m)
        {
            if (m.HasProperty("umbracoExtension"))
            {
                return m.GetValue<string>("umbracoExtension");
            }

            return null;
        }

        private static readonly Dictionary<string, string> fileTypes = new Dictionary<string, string>()
        {
            { "pdf", "PDF Document" },
            { "doc", "Word Document" },
            { "docx", "Word Document" },
            { "txt", "Text File" },
            { "rtf", "Rich Text" },
            { "xls", "Spreadsheet" },
            { "xlsx", "Spreadsheet" },
            { "csv", "Spreadsheet" },
            { "ppt", "Presentation" },
            { "pptx", "Presentation" },
            { "zip" , "Zip Archive"},
            { "jpg", "JPEG Image" },
            { "jpeg", "JPEG Image" },
            { "png", "PNG Image" },
            { "gif", "GIF Image" },
            { "bmp", "Bitmap Image" },
            { "svg", "Vector Image" },
            { "mp3", "Audio" },
            { "mp4", "Video" },
            { "mov", "Video" },
            { "wmv", "Video" },
            { "avi", "Video" },
            { "webm", "Video" },
            { "ogv", "Video" },
            { "xml", "XML" }
        };
    }
}