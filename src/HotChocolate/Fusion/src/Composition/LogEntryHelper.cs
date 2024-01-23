using System.Security;
using HotChocolate.Language;
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
            LogSeverity.Warning,
            coordinate,
            schema: schema);

    public static LogEntry RenameMemberNotFound(
        SchemaCoordinate coordinate,
        Schema schema)
        => new LogEntry(
            string.Format(LogEntryHelper_RenameMemberNotFound, coordinate),
            LogEntryCodes.RemoveMemberNotFound,
            LogSeverity.Warning,
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
            LogSeverity.Error,
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
            extension: new[] { sourceKind, targetKind, });

    public static LogEntry OutputFieldArgumentMismatch(
        SchemaCoordinate coordinate,
        OutputField field)
        => new LogEntry(
            LogEntryHelper_OutputFieldArgumentMismatch,
            code: LogEntryCodes.OutputFieldArgumentMismatch,
            severity: LogSeverity.Error,
            coordinate: coordinate,
            member: field);

    public static LogEntry OutputFieldArgumentSetMismatch(
        SchemaCoordinate coordinate,
        OutputField field,
        IReadOnlyList<string> targetArgs,
        IReadOnlyList<string> sourceArgs)
        => new LogEntry(
            string.Format(
                LogEntryHelper_OutputFieldArgumentSetMismatch,
                coordinate.ToString(),
                string.Join(", ", targetArgs),
                string.Join(", ", sourceArgs)),
            code: LogEntryCodes.OutputFieldArgumentSetMismatch,
            severity: LogSeverity.Error,
            coordinate: coordinate,
            member: field);

    public static LogEntry FieldDependencyCannotBeResolved(
        SchemaCoordinate coordinate,
        FieldNode dependency,
        Schema schema)
        => new LogEntry(
            string.Format(
                LogEntryHelper_FieldDependencyCannotBeResolved,
                dependency),
            severity: LogSeverity.Error,
            code: LogEntryCodes.FieldDependencyCannotBeResolved,
            coordinate: coordinate,
            schema: schema);

    public static LogEntry TypeNotDeclared(MissingType type, Schema schema)
        => new(
            string.Format(LogEntryHelper_TypeNotDeclared, type.Name, schema.Name),
            LogEntryCodes.TypeNotDeclared,
            severity: LogSeverity.Error,
            coordinate: new SchemaCoordinate(type.Name),
            member: type,
            schema: schema);

    public static LogEntry OutputFieldTypeMismatch(
        SchemaCoordinate schemaCoordinate, 
        OutputField source, 
        IType targetType, 
        IType sourceType)
        => new(
            string.Format(
                LogEntryHelper_OutputFieldTypeMismatch,
                schemaCoordinate,
                targetType.ToTypeNode().ToString(),
                sourceType.ToTypeNode().ToString()),
            LogEntryCodes.TypeKindMismatch,
            severity: LogSeverity.Error,
            coordinate: schemaCoordinate,
            member: source,
            extension: new[] { targetType, sourceType, });
    
    public static LogEntry InputFieldTypeMismatch(
        SchemaCoordinate schemaCoordinate, 
        InputField source, 
        IType targetType, 
        IType sourceType)
        => new(
            string.Format(
                LogEntryHelper_OutputFieldTypeMismatch,
                schemaCoordinate,
                targetType.ToTypeNode().ToString(),
                sourceType.ToTypeNode().ToString()),
            LogEntryCodes.TypeKindMismatch,
            severity: LogSeverity.Error,
            coordinate: schemaCoordinate,
            member: source,
            extension: new[] { targetType, sourceType, });
    
    public static LogEntry RootTypeNameMismatch(
        OperationType operationType,
        string fusionRootTypeName,
        string subgraphRootTypeName,
        string subgraphName)
        => new(
            string.Format(
                LogEntryHelper_RootTypeNameMismatch,
                operationType.ToString().ToLowerInvariant(),
                fusionRootTypeName,
                subgraphRootTypeName,
                subgraphName),
            LogEntryCodes.TypeKindMismatch,
            severity: LogSeverity.Error);
}

static file class LogEntryCodes
{
    public const string RemoveMemberNotFound = "HF0001";

    public const string DirectiveArgumentMissing = "HF0002";

    public const string DirectiveArgumentValueInvalid = "HF0003";

    public const string TypeKindMismatch = "HF0004";

    public const string OutputFieldArgumentMismatch = "HF0005";

    public const string OutputFieldArgumentSetMismatch = "HF0006";

    public const string CoordinateNotAllowedForRequirements = "HF0007";

    public const string FieldDependencyCannotBeResolved = "HF0008";
    
    public const string TypeNotDeclared = "HF0009";
    public const string RootNameMismatch = "HF0010";
}
