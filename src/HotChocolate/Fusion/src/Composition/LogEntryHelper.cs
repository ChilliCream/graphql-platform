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
            schema: schema);

    public static LogEntry RenameMemberNotFound(
        SchemaCoordinate coordinate,
        Schema schema)
        => new LogEntry(
            string.Format(LogEntryHelper_RenameMemberNotFound, coordinate),
            LogEntryCodes.RemoveMemberNotFound,
            LogEntryKind.Warning,
            coordinate,
            schema: schema);

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
            member: directive,
            schema: schema);

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
            member: directive,
            schema: schema);

    public static LogEntry UnableToMergeType(
        TypeGroup typeGroup)
        => new LogEntry(
            string.Format(
                LogEntryHelper_UnableToMergeType,
                typeGroup.Name),
            LogEntryCodes.DirectiveArgumentValueInvalid,
            extension: typeGroup);

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
            extension: new[] { sourceKind, targetKind });

    public static LogEntry OutputFieldArgumentMismatch(
        SchemaCoordinate coordinate,
        OutputField field)
        => new LogEntry(
            LogEntryHelper_OutputFieldArgumentMismatch,
            code: LogEntryCodes.OutputFieldArgumentMismatch,
            kind: LogEntryKind.Error,
            coordinate: coordinate,
            member: field);

    public static LogEntry OutputFieldArgumentSetMismatch(
        SchemaCoordinate coordinate,
        OutputField field)
        => new LogEntry(
            LogEntryHelper_OutputFieldArgumentSetMismatch,
            code: LogEntryCodes.OutputFieldArgumentSetMismatch,
            kind: LogEntryKind.Error,
            coordinate: coordinate,
            member: field);
}

internal static class LogEntryCodes
{
    public const string RemoveMemberNotFound = "HF0001";

    public const string DirectiveArgumentMissing = "HF0002";

    public const string DirectiveArgumentValueInvalid = "HF0003";

    public const string TypeKindMismatch = "HF0004";

    public const string OutputFieldArgumentMismatch = "HF0005";

    public const string OutputFieldArgumentSetMismatch = "HF0006";
}
