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
}
