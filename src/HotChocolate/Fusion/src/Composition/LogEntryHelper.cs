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
            LogEntryKind.Warning,
            coordinate,
            Schema: schema);

    public static LogEntry RenameMemberNotFound(
        SchemaCoordinate coordinate,
        Schema schema)
        => new LogEntry(
            string.Format(LogEntryHelper_RenameMemberNotFound, coordinate),
            LogEntryCodes.RemoveMemberNotFound,
            LogEntryKind.Warning,
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
            LogEntryKind.Error,
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

    public static LogEntry UnableToMergeType(
        TypeGroup typeGroup)
        => new LogEntry(
            string.Format(
                LogEntryHelper_UnableToMergeType,
                typeGroup.Name),
            LogEntryCodes.DirectiveArgumentValueInvalid,
            Extension: typeGroup);

    public static LogEntry MergeTypeKindDoesNotMatch(
        INamedType type,
        TypeKind sourceKind,
        TypeKind targetKind)
        => new LogEntry(
            string.Format(
                LogEntryHelper_MergeTypeKindDoesNotMatch,
                type.Name,
                sourceKind,
                targetKind),
            LogEntryCodes.TypeKindMismatch,
            Extension: new[] { sourceKind, targetKind });
}

public static class LogEntryCodes
{
    public const string RemoveMemberNotFound = "HF0001";

    public const string DirectiveArgumentMissing = "HF0002";

    public const string DirectiveArgumentValueInvalid = "HF0003";

    public const string TypeKindMismatch = "HF0004";
}
