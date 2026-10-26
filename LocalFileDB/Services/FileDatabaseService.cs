using System.IO;
using System.Text.Json;
using LocalFileDB.Models;

namespace LocalFileDB.Services
{
    /// <summary>
    /// Reads, writes, and creates entries for JSON file catalogs.
    /// </summary>
    public sealed class FileDatabaseService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".bmp", ".gif", ".heic", ".heif", ".jpeg", ".jpg", ".png", ".tif", ".tiff", ".webp"
        };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".avi", ".m4v", ".mkv", ".mov", ".mp4", ".mpeg", ".mpg", ".webm", ".wmv"
        };

        /// <summary>
        /// Loads and validates a catalog from a JSON file.
        /// </summary>
        public FileDatabase Load(string databasePath)
        {
            var json = File.ReadAllText(databasePath);
            var database = JsonSerializer.Deserialize<FileDatabase>(json, JsonOptions);
            if (database is null || string.IsNullOrWhiteSpace(database.Name))
            {
                throw new InvalidDataException("The database must contain a non-empty name.");
            }

            if (string.IsNullOrWhiteSpace(database.RootDirectory))
            {
                throw new InvalidDataException("The database must contain a rootDirectory.");
            }

            return database;
        }

        /// <summary>
        /// Writes the catalog to its JSON file.
        /// </summary>
        public void Save(string databasePath, FileDatabase database)
        {
            var json = JsonSerializer.Serialize(database, JsonOptions);
            File.WriteAllText(databasePath, json);
        }

        /// <summary>
        /// Creates a new entry with a root-relative path when possible.
        /// </summary>
        public FileDatabaseEntry CreateEntry(string filePath, string rootDirectory)
        {
            var fullPath = Path.GetFullPath(filePath);
            var fullRootDirectory = Path.GetFullPath(rootDirectory);
            var useFullPath = !IsWithinDirectory(fullPath, fullRootDirectory);
            var extension = Path.GetExtension(fullPath);
            var mediaType = ImageExtensions.Contains(extension)
                ? "image"
                : VideoExtensions.Contains(extension) ? "video" : "unknown";

            return new FileDatabaseEntry
            {
                Path = useFullPath ? fullPath : Path.GetRelativePath(fullRootDirectory, fullPath),
                UseFullPath = useFullPath,
                MediaType = mediaType,
                Tags = [],
                AdditionalInfo = []
            };
        }

        /// <summary>
        /// Resolves an entry's stored path against its database root when needed.
        /// </summary>
        public static string ResolvePath(FileDatabaseEntry entry, string rootDirectory)
        {
            var path = entry.UseFullPath ? entry.Path : Path.Combine(rootDirectory, entry.Path);
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Checks directory containment without matching similarly prefixed sibling folders.
        /// </summary>
        private static bool IsWithinDirectory(string filePath, string directoryPath)
        {
            if (string.Equals(filePath, directoryPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var directoryPrefix = Path.EndsInDirectorySeparator(directoryPath)
                ? directoryPath
                : directoryPath + Path.DirectorySeparatorChar;
            return filePath.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
