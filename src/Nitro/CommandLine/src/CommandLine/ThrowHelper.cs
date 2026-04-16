namespace ChilliCream.Nitro.CommandLine;

internal static class ThrowHelper
{
    // TODO: Get rid of this
    public static ExitException Exit(string message)
    {
        return new ExitException(message);
    }

    public static ExitException MissingRequiredOption(string optionName)
        => Exit($"The '{optionName}' option is required in non-interactive mode.");

    // TODO: Challenge these
    public static Exception NoPageInfoFound()
        => ThereWasAnIssueWithTheRequest("No page info found in the response.");

    public static Exception CouldNotSelectEdges()
        => ThereWasAnIssueWithTheRequest("Could not select edges.");

    public static Exception NoClientSelected() => Exit("You did not select a client!");

    public static ExitException MutationReturnedNoData()
        => Exit("The GraphQL mutation completed without errors, but the server did not return the expected data.");

    public static Exception ThereWasAnIssueWithTheRequest(string? additional = null)
        => new ExitException(
            $"There was an issue with the request to the server.\n{additional ?? ""}");
}
