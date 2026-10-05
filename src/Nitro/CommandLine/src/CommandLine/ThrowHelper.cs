using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;
using System.Globalization;

namespace ChilliCream.Nitro.CommandLine;

internal static class ThrowHelper
{
    public static ExitException Exit(string message)
    {
        return new ExitException(message);
    }

    public static ExitException MissingRequiredOption(string optionName)
        => Exit($"Missing required option '{optionName}'.");

    public static ExitException MissingRequiredArgument(string argumentName)
        => Exit($"Missing required argument '{argumentName}'.");

    public static Exception NoPageInfoFound()
        => new ExitException("No page info found in the response.");

    public static Exception CouldNotSelectEdges()
        => new ExitException("Could not select edges.");

    public static Exception NoClientSelected() => Exit("You did not select a client!");

    public static ExitException MutationReturnedNoData()
        => Exit("The GraphQL mutation completed without errors, but the server did not return the expected data.");

    public static ArgumentOutOfRangeException NegativeLimit(int limit)
        => new(nameof(limit), limit, "Limit must be zero or greater.");

    public static ArgumentOutOfRangeException UnsupportedSeverityLevel(string severity)
        => new(nameof(severity), severity, "Unsupported severity level.");

    public static ArgumentOutOfRangeException UnsupportedFilterNode(FilterNode? node)
        => new(nameof(node));

    public static ArgumentOutOfRangeException UnsupportedFilterComparisonOperator(
        FilterComparisonOperator comparison)
        => new(nameof(comparison));

    public static FilterParseException InvalidFilterSyntax(string message, int position)
        => new(message, position + 1);

    public static ArgumentException UnknownAgentHarness(string harness)
        => new($"'{harness}' is not an agent harness.", nameof(harness));

    public static ExitException UnknownMailRecipient(string name)
        => Exit($"Unknown agent '{name}'. Look the name up with 'nitro agent list'.");

    public static ExitException DeletedMailRecipient(string name)
        => Exit($"Agent '{name}' was deleted. Look the name up with 'nitro agent list'.");

    public static ExitException NoReplyRecipientsRemaining(IEnumerable<(string Name, bool WasDeleted)> skipped)
    {
        var reasons = skipped.Select(
            recipient => recipient.WasDeleted
                ? $"'{recipient.Name}' was deleted"
                : $"'{recipient.Name}' is unknown");

        return Exit($"No recipients left: {string.Join(", ", reasons)}.");
    }

    public static ExitException NoAgentWorkspaceToBackUp()
        => Exit(
            "No agent workspace found to back up. Neither a '.nitro' directory nor a "
            + "'.git/nitro' directory exists.");

    public static ExitException ArchiveAlreadyExists(string archivePath)
        => Exit($"The archive '{archivePath}' already exists. Use --force to overwrite it.");

    public static ExitException ArchivePathIsDirectory(string archivePath)
        => Exit($"The archive path '{archivePath}' is a directory.");

    public static ExitException ArchiveInsideBackedUpDirectory(string archivePath, string directory)
        => Exit($"The archive '{archivePath}' is inside '{directory}', which is part of the backup.");

    public static ExitException ArchiveNotFound(string archivePath)
        => Exit($"The archive '{archivePath}' does not exist.");

    public static ExitException ArchiveNotReadable(string archivePath)
        => Exit($"The archive '{archivePath}' is not a valid zip file.");

    public static ExitException ArchiveManifestMissing(string archivePath)
        => Exit($"The archive '{archivePath}' has no 'manifest.json', so it is not an agent workspace backup.");

    public static ExitException ArchiveManifestInvalid(string archivePath)
        => Exit($"The 'manifest.json' in archive '{archivePath}' is not a valid agent workspace manifest.");

    public static ExitException ArchiveFormatUnsupported(long formatVersion, int supportedVersion)
        => Exit(
            $"The archive format version {formatVersion} is not supported. "
            + $"This version of Nitro reads format version {supportedVersion}.");

    public static ExitException ArchiveEntryInvalid(string entryName)
        => Exit(
            $"The archive entry '{entryName}' is not allowed. Entries must be relative paths "
            + "under a root listed in the manifest, without '..' segments.");

    public static ExitException ArchiveDatabaseInvalid(string entryName)
        => Exit($"The archive entry '{entryName}' is not a valid agent database.");

    public static ExitException ArchiveDatabaseNewer(string entryName, long version, int supportedVersion)
        => Exit(
            $"The agent database '{entryName}' in the archive has schema version {version}, which is "
            + $"newer than the version {supportedVersion} this version of Nitro supports. Update Nitro to restore it.");

    public static ExitException ArchiveGitRootWithoutRepository(string archivePath)
        => Exit(
            $"The archive '{archivePath}' contains a '.git/nitro' directory, but the current directory "
            + "is not inside a git repository.");

    public static ExitException ArchiveInsideRestoredDirectory(string archivePath, string directory)
        => Exit($"The archive '{archivePath}' is inside '{directory}', which restore deletes.");

    public static ExitException RestoreRequiresForce()
        => Exit("Use --force to restore without confirmation.");

    public static ExitException ActiveAgentsBlockRestore(IEnumerable<string> agents)
        => Exit(
            $"Agents are active in this workspace and hold its database open: {string.Join(", ", agents)}. "
            + "Use --force to restore anyway.");

    public static ExitException MailWakeDaemonBlocksRestore(string workspaceDirectory, DateTimeOffset leaseExpiresAt)
        => Exit(
            $"A mail wake daemon holds the database in '{workspaceDirectory}' "
            + $"(lease until {leaseExpiresAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}Z). "
            + "Close the `nitro agent` board or the process running it, then restore again.");

    public static ExitException RestoreActivityUnknown(IEnumerable<string> databasePaths)
        => Exit(
            $"Could not check whether agents are using the workspace database: {string.Join(", ", databasePaths)}. "
            + "Use --force to restore anyway.");
}
