using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine;

internal static class Messages
{
    public static string InvalidArchive(string message)
        => "The server received an invalid archive. "
            + "This indicates a bug in the tooling. "
            + "Please notify ChilliCream. "
            + "Error received: "
            + message;

    public static string UnexpectedMutationError()
        => "Unexpected mutation error.";

    public static string UnexpectedMutationError(IError error)
        => $"Unexpected mutation error: {error.Message}";

    public static string SchemaFileDoesNotExist(string path) => $"Schema file '{path}' does not exist.";

    public static string ArchiveFileDoesNotExist(string path) => $"Archive file '{path}' does not exist.";

    public static string LegacyArchiveFileDoesNotExist(string path) => $"Legacy archive file '{path}' does not exist.";

    public static string LegacyArchiveAsCompositionBase(string filePath)
        => $"Using legacy v1 archive '{filePath}' as the composition base.";

    public static string LegacyArchiveFromRegistryAsCompositionBase(string stageName)
        => $"No .far archive found on stage '{stageName}'. Using downloaded legacy v1 archive as the composition base.";

    public static string FailedToOpenLegacyArchive(string filePath, string detail)
        => $"Failed to open legacy v1 archive '{filePath}': {detail}";

    public static string LegacyArchiveCorrupt(string filePath, string detail)
        => $"Legacy v1 archive '{filePath}' is corrupt or malformed: {detail}";

    public static string LegacyArchiveSchemaExtensionsNotSupported(string sourceSchemaName)
        => $"Legacy archive source schema '{sourceSchemaName}' contains schema extensions which are not supported in .far archives.";

    public static string OperationsFileDoesNotExist(string path) => $"Operations file '{path}' does not exist.";

    public static string ExtensionFileDoesNotExist(string path) => $"Extension file '{path}' does not exist.";

    public const string UnknownServerResponse =
        "Unknown server response. Consider updating the CLI.";

    public const string NoFusionRequestId =
        "No request ID was provided and no request ID was found in the cache. Please provide a request ID.";

    public const string ForcePushEnabled = "Force push is enabled.";

    public const string Validating = "Validating...";

    public const string ValidationPassed = "Passed validation.";

    public const string ValidationFailed = "Failed validation.";

    public const string RequestReadyForProcessing =
        "Your request is ready for processing.";

    public const string RequestBeingProcessed =
        "Your request is being processed.";

    public const string WaitingForApproval =
        "Waiting for approval. Approve in Nitro to continue.";

    public const string RequestApproved =
        "Your request has been approved.";

    public static string QueuedAtPosition(int position)
        => $"Your request is queued at position {position}.";
}
