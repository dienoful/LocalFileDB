using System.IO;
using System.Text.Json;
using LocalFileDB.Models;

namespace LocalFileDB.Services
{
    /// <summary>
    /// Persists user-level application settings outside the catalog files.
    /// </summary>
    public sealed class AppSettingsStore
    {
        private const int MaximumRecentDatabases = 10;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private readonly string _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalFileDB",
            "settings.json");

        /// <summary>
        /// Loads recent database paths, returning an empty list when settings are unavailable.
        /// </summary>
        public List<string> LoadRecentDatabasePaths()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    return [];
                }

                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsFilePath), JsonOptions);
                return NormalizeRecentPaths(settings?.RecentDatabasePaths ?? []);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                return [];
            }
        }

        /// <summary>
        /// Saves the normalized recent database list to the per-user settings file.
        /// </summary>
        public void SaveRecentDatabasePaths(IEnumerable<string> paths)
        {
            var settingsDirectory = Path.GetDirectoryName(_settingsFilePath)!;
            Directory.CreateDirectory(settingsDirectory);

            var settings = new AppSettings
            {
                RecentDatabasePaths = NormalizeRecentPaths(paths)
            };

            File.WriteAllText(_settingsFilePath, JsonSerializer.Serialize(settings, JsonOptions));
        }

        /// <summary>
        /// Moves a database path to the front and trims the list to its maximum size.
        /// </summary>
        public static List<string> AddRecentDatabase(IEnumerable<string> paths, string databasePath)
        {
            var normalizedPath = Path.GetFullPath(databasePath);
            var updatedPaths = NormalizeRecentPaths(paths);
            updatedPaths.RemoveAll(path => string.Equals(path, normalizedPath, StringComparison.OrdinalIgnoreCase));
            updatedPaths.Insert(0, normalizedPath);

            if (updatedPaths.Count > MaximumRecentDatabases)
            {
                updatedPaths.RemoveRange(MaximumRecentDatabases, updatedPaths.Count - MaximumRecentDatabases);
            }

            return updatedPaths;
        }

        /// <summary>
        /// Normalizes paths, removes invalid entries and duplicates, and enforces the list limit.
        /// </summary>
        private static List<string> NormalizeRecentPaths(IEnumerable<string> paths)
        {
            var normalizedPaths = new List<string>();
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                string normalizedPath;
                try
                {
                    normalizedPath = Path.GetFullPath(path);
                }
                catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
                {
                    continue;
                }

                if (!normalizedPaths.Contains(normalizedPath, StringComparer.OrdinalIgnoreCase))
                {
                    normalizedPaths.Add(normalizedPath);
                }

                if (normalizedPaths.Count == MaximumRecentDatabases)
                {
                    break;
                }
            }

            return normalizedPaths;
        }
    }

}
