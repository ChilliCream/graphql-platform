namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Defines the on-disk layout of a mail workspace and shared helpers for it.
/// </summary>
internal static class MailWorkspace
{
    public const string RootDirectoryName = ".nitro";
    public const string MailDirectoryName = "mail";
    public const string DatabaseFileName = "mail.db";
    public const string GitIgnoreFileName = ".gitignore";
    public const string DisplayPath = RootDirectoryName + "/" + MailDirectoryName;

    /// <summary>
    /// Ignores everything in the mail workspace: nothing in it is ever
    /// committed, unlike the task tracker's JSONL export.
    /// </summary>
    public const string GitIgnoreContent = "*";

    public static string GetDirectory(string baseDirectory)
        => Path.Combine(baseDirectory, RootDirectoryName, MailDirectoryName);

    public static string GetDatabasePath(string workspaceDirectory)
        => Path.Combine(workspaceDirectory, DatabaseFileName);

    /// <summary>
    /// Finds the nearest workspace directory at or above the given directory.
    /// Returns null when no workspace exists.
    /// </summary>
    public static string? Find(IFileSystem fileSystem, string startDirectory)
    {
        for (var directory = startDirectory;
            !string.IsNullOrEmpty(directory);
            directory = Path.GetDirectoryName(directory))
        {
            var workspaceDirectory = GetDirectory(directory);

            if (fileSystem.FileExists(GetDatabasePath(workspaceDirectory)))
            {
                return workspaceDirectory;
            }
        }

        return null;
    }
}
