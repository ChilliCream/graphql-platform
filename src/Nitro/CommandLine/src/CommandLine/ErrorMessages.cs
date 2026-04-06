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

    public static string SourceSchemaFileDoesNotExist(string path) => $"Source schema file '{path}' does not exist.";

    public static string ArchiveFileDoesNotExist(string path) => $"Archive file '{path}' does not exist.";

    public const string UnknownServerResponse =
        "Unknown server response. Consider updating the CLI.";
}
