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
            extension: new[] { sourceKind, targetKind });

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
            SpecErrorCodes.FieldTypeKindMismatch,
            severity: LogSeverity.Error,
            coordinate: schemaCoordinate,
            member: source,
            extension: new[] { targetType, sourceType });
    
    public static LogEntry ArgumentTypeMismatch(
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
            SpecErrorCodes.ArgumentTypeKindMismatch,
            severity: LogSeverity.Error,
            coordinate: schemaCoordinate,
            member: source,
            extension: new[] { targetType, sourceType });
    
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
            SpecErrorCodes.FieldTypeKindMismatch,
            severity: LogSeverity.Error,
            coordinate: schemaCoordinate,
            member: source,
            extension: new[] { targetType, sourceType });
    
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

    public static LogEntry DifferentTypeKindsCannotBeMerged(
        TypeGroup typeGroup)
    {
        var expectedKind = typeGroup.Parts[0].Type.Kind;
        
        return DifferentTypeKindsCannotBeMerged(
            typeGroup.Name,
            typeGroup.Parts.Select(t => t.Type.Kind).Distinct(),
            typeGroup.Parts.First(t => t.Type.Kind != expectedKind).Schema);
    }

    public static LogEntry DifferentTypeKindsCannotBeMerged(
        string typeName,
        IEnumerable<TypeKind> typeKinds,
        Schema violatingSchema)
        => new LogEntry(
            string.Format(
                "There are different type kinds registered with the name `{0}`. The following type kinds where found {1}.",
                typeName,
                string.Join(", ", typeKinds)),
            SpecErrorCodes.TypeKindMismatch,
            LogSeverity.Error,
            new SchemaCoordinate(typeName),
            violatingSchema);
    
    public static LogEntry EnumValuesDifferAcrossSubgraphs(
        string typeName,
        IEnumerable<string> expectedValues,
        IEnumerable<string> unexpectedValues)
        => new LogEntry(
            string.Format(
                "The composition expected the enum type `{0}` to have the enum values `{1}` across all subgraphs. " + 
                "But found the following values on some subgraphs `{2}`.",
                typeName,
                string.Join(",", expectedValues.OrderBy(t => t)),
                string.Join(",", unexpectedValues.OrderBy(t => t))),
            SpecErrorCodes.EnumValuesDiffer,
            LogSeverity.Error,
            new SchemaCoordinate(typeName));
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

static file class SpecErrorCodes
{
    public const string TypeKindMismatch = "F0001";
    
    public const string FieldTypeKindMismatch = "F0002";
    
    public const string ArgumentTypeKindMismatch = "F0004";
    
    public const string EnumValuesDiffer = "F0003";

}
