using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine;

internal static class ErrorMessages
{
    public static string InvalidArchive(string message)
        => "The server received an invalid archive. "
            + "This indicates a bug in the tooling. "
            + "Please notify ChilliCream."
            + Environment.NewLine
            + "Error received: "
            + message;

    public static string UnexpectedMutationError()
        => "Unexpected mutation error.";

    public static string UnexpectedMutationError(IError error)
        => $"Unexpected mutation error': {error.Message}";
}
