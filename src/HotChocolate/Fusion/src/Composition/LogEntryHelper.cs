using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.Properties.CompositionResources;

namespace HotChocolate.Fusion.Composition;

internal static class LogEntryHelper
{
    public static LogEntry RemoveMemberNotFound(
        SchemaCoordinate coordinate,
        Schema schema)
        => new LogEntry(
            string.Format(LogEntryHelper_RemoveMemberNotFound, coordinate),
            LogEntryCodes.RemoveMemberNotFound,
            coordinate,
            Schema: schema);

    public static LogEntry RenameMemberNotFound(
        SchemaCoordinate coordinate,
        Schema schema)
        => new LogEntry(
            string.Format(LogEntryHelper_RenameMemberNotFound, coordinate),
            LogEntryCodes.RemoveMemberNotFound,
            coordinate,
            Schema: schema);

    public static LogEntry DirectiveArgumentMissing(
        string argumentName,
        Directive directive,
        Schema schema)
        => new LogEntry(
            string.Format(
                LogEntryHelper_DirectiveArgumentMissing,
                argumentName,
                directive.Name),
            LogEntryCodes.DirectiveArgumentMissing,
            Member: directive,
            Schema: schema);

    public static LogEntry DirectiveArgumentValueInvalid(
        string argumentName,
        Directive directive,
        Schema schema)
        => new LogEntry(
            string.Format(
                LogEntryHelper_DirectiveArgumentValueInvalid,
                argumentName,
                directive.Name),
            LogEntryCodes.DirectiveArgumentValueInvalid,
            Member: directive,
            Schema: schema);
}

public static class LogEntryCodes
{
    public const string RemoveMemberNotFound = "HF0001";

    public const string DirectiveArgumentMissing = "HF0002";

    public const string DirectiveArgumentValueInvalid = "HC0003";
}
