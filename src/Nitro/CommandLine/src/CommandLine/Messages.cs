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

    public static string OperationsFileDoesNotExist(string path) => $"Operations file '{path}' does not exist.";

    public static string ExtensionFileDoesNotExist(string path) => $"Extension file '{path}' does not exist.";

    public const string UnknownServerResponse =
        "Unknown server response. Consider updating the CLI.";

    public const string NoFusionRequestId =
        "No request ID was provided and no request ID was found in the cache. Please provide a request ID.";

    public const string ForcePushEnabled = "Force push is enabled.";

    public const string Validating = "Validating...";

    public const string ValidationPassed = "Validation successful.";

    public const string ValidationFailed = "Validation did not pass.";

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
