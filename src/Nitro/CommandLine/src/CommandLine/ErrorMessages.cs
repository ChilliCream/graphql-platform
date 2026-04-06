using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine;

internal static class ErrorMessages
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

    public const string UnknownServerResponse =
        "Unknown server response. Consider updating the CLI.";

    public const string NoFusionRequestId =
        "No request ID was provided and no request ID was found in the cache. Please provide a request ID.";
}
