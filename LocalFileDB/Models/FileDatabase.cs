namespace LocalFileDB.Models
{
    /// <summary>
    /// Stores the name, root directory, and file entries in a JSON catalog.
    /// </summary>
    public sealed class FileDatabase
    {
        /// <summary>
        /// Gets the user-visible catalog name.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the root used to resolve entries stored with relative paths.
        /// </summary>
        public string RootDirectory { get; init; } = string.Empty;

        /// <summary>
        /// Gets the files and their catalog metadata.
        /// </summary>
        public List<FileDatabaseEntry>? Files { get; init; } = [];
    }

    /// <summary>
    /// Stores the path and metadata for one catalog file.
    /// </summary>
    public sealed class FileDatabaseEntry
    {
        /// <summary>
        /// Gets the full or root-relative path according to <see cref="UseFullPath"/>.
        /// </summary>
        public string Path { get; init; } = string.Empty;

        /// <summary>
        /// Gets whether <see cref="Path"/> is absolute instead of relative to the catalog root.
        /// </summary>
        public bool UseFullPath { get; init; }

        /// <summary>
        /// Gets the media classification, such as image or video.
        /// </summary>
        public string MediaType { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the tags assigned to the file.
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// Gets additional metadata that can be extended without changing the core fields.
        /// </summary>
        public Dictionary<string, System.Text.Json.JsonElement>? AdditionalInfo { get; init; } = [];
    }
}
