using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Logging;

internal static class LogEntryHelper
{
    public static LogEntry DisallowedInaccessibleBuiltInScalar(
        ScalarTypeDefinition scalar,
        SchemaDefinition schema)
        => new(
            string.Format(LogEntryHelper_DisallowedInaccessibleBuiltInScalar, scalar.Name),
            LogEntryCodes.DisallowedInaccessible,
            LogSeverity.Error,
            new SchemaCoordinate(scalar.Name),
            scalar,
            schema);

    public static LogEntry DisallowedInaccessibleIntrospectionType(
        INamedTypeDefinition type,
        SchemaDefinition schema)
        => new(
            string.Format(LogEntryHelper_DisallowedInaccessibleIntrospectionType, type.Name),
            LogEntryCodes.DisallowedInaccessible,
            LogSeverity.Error,
            new SchemaCoordinate(type.Name),
            type,
            schema);

    public static LogEntry DisallowedInaccessibleIntrospectionField(
        OutputFieldDefinition field,
        string typeName,
        SchemaDefinition schema)
        => new(
            string.Format(
                LogEntryHelper_DisallowedInaccessibleIntrospectionField,
                field.Name,
                typeName),
            LogEntryCodes.DisallowedInaccessible,
            LogSeverity.Error,
            new SchemaCoordinate(typeName, field.Name),
            field,
            schema);

    public static LogEntry DisallowedInaccessibleIntrospectionArgument(
        InputFieldDefinition argument,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argument.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_DisallowedInaccessibleIntrospectionArgument,
                argument.Name,
                coordinate),
            LogEntryCodes.DisallowedInaccessible,
            LogSeverity.Error,
            coordinate,
            argument,
            schema);
    }

    public static LogEntry DisallowedInaccessibleDirectiveArgument(
        InputFieldDefinition argument,
        string directiveName,
        SchemaDefinition schema)
        => new(
            string.Format(
                LogEntryHelper_DisallowedInaccessibleDirectiveArgument,
                argument.Name,
                directiveName),
            LogEntryCodes.DisallowedInaccessible,
            LogSeverity.Error,
            new SchemaCoordinate(directiveName, argumentName: argument.Name, ofDirective: true),
            schema: schema);

    public static LogEntry ExternalArgumentDefaultMismatch(string fieldName, string typeName, string argumentName)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);
        return new LogEntry(
            string.Format(
                LogEntryHelper_ExternalArgumentDefaultMismatch,
                coordinate),
            LogEntryCodes.ExternalArgumentDefaultMismatch,
            LogSeverity.Error,
            coordinate);
    }

    public static LogEntry ExternalMissingOnBase(string fieldName, string typeName)
        => new(
            string.Format(
                LogEntryHelper_ExternalMissingOnBase,
                fieldName,
                typeName),
            LogEntryCodes.ExternalMissingOnBase,
            LogSeverity.Error,
            new SchemaCoordinate(typeName, fieldName));

    public static LogEntry OutputFieldTypesNotMergeable(string fieldName, string typeName)
        => new(
            string.Format(
                LogEntryHelper_OutputFieldTypesNotMergeable,
                fieldName,
                typeName),
            LogEntryCodes.OutputFieldTypesNotMergeable,
            LogSeverity.Error,
            new SchemaCoordinate(typeName, fieldName));
}
