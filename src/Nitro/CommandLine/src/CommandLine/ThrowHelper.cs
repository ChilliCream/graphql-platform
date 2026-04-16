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
}
