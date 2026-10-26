namespace LocalFileDB.Models
{
    /// <summary>
    /// Defines the values persisted in the application settings file.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        /// Gets the recent catalog paths in most-recent-first order.
        /// </summary>
        public List<string>? RecentDatabasePaths { get; init; } = [];
    }
}
