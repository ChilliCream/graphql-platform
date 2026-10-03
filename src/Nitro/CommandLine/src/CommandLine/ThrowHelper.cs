using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

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
}
