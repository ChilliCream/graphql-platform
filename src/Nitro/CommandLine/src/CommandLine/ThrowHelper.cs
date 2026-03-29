using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine;

internal static class ThrowHelper
{
    public static ExitException Exit(string message)
    {
        return new ExitException(message);
    }

    public static ExitException MissingRequiredOption(string optionName)
        => Exit($"The '{optionName}' option is required in non-interactive mode.");

    public static ExitException NoDefaultWorkspace() => new(
        $"You are not logged in. Run {"nitro login".AsCommand()} to sign in or manually specify the '{OptionalWorkspaceIdOption.OptionName}' option (if available).");

    public static Exception NoPageInfoFound()
        => ThereWasAnIssueWithTheRequest("No page info found in the response.");

    public static Exception CouldNotSelectEdges()
        => ThereWasAnIssueWithTheRequest("Could not select edges.");

    public static Exception NoApiSelected() => Exit("You did not select an API!");

    public static Exception NoClientSelected() => Exit("You did not select a client!");

    public static Exception NoOpenApiCollectionSelected() => Exit("You did not select an OpenAPI collection!");

    public static Exception NoMcpFeatureCollectionSelected() => Exit("You did not select an MCP Feature Collection!");

    public static Exception ThereWasAnIssueWithTheRequest(string? additional = null)
        => new ExitException(
            $"There was an issue with the request to the server.\n{additional ?? ""}");
}
