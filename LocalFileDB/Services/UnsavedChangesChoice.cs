namespace LocalFileDB.Services
{
    /// <summary>
    /// Describes the user's choice when a database has unsaved changes.
    /// </summary>
    public enum UnsavedChangesChoice
    {
        /// <summary>
        /// Save the current database before continuing.
        /// </summary>
        Save,

        /// <summary>
        /// Discard pending edits and continue.
        /// </summary>
        Discard,

        /// <summary>
        /// Cancel the close or database switch.
        /// </summary>
        Cancel
    }
}
