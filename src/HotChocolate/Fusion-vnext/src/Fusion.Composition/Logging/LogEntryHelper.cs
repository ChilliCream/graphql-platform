using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Logging;

internal static class LogEntryHelper
{
    public static LogEntry DisallowedInaccessibleBuiltInScalar(
        ScalarTypeDefinition scalar,
        SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_DisallowedInaccessibleBuiltInScalar,
                scalar.Name,
                schema.Name),
            LogEntryCodes.DisallowedInaccessible,
            LogSeverity.Error,
            new SchemaCoordinate(scalar.Name),
            scalar,
            schema);
    }

    public static LogEntry DisallowedInaccessibleIntrospectionType(
        INamedTypeDefinition type,
        SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_DisallowedInaccessibleIntrospectionType,
                type.Name,
                schema.Name),
            LogEntryCodes.DisallowedInaccessible,
            LogSeverity.Error,
            new SchemaCoordinate(type.Name),
            type,
            schema);
    }

    public static LogEntry DisallowedInaccessibleIntrospectionField(
        OutputFieldDefinition field,
        string typeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, field.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_DisallowedInaccessibleIntrospectionField,
                coordinate,
                schema.Name),
            LogEntryCodes.DisallowedInaccessible,
            LogSeverity.Error,
            coordinate,
            field,
            schema);
    }

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
                coordinate,
                schema.Name),
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
    {
        var coordinate = new SchemaCoordinate(
            directiveName,
            argumentName: argument.Name,
            ofDirective: true);

        return new LogEntry(
            string.Format(
                LogEntryHelper_DisallowedInaccessibleDirectiveArgument,
                coordinate,
                schema.Name),
            LogEntryCodes.DisallowedInaccessible,
            LogSeverity.Error,
            coordinate,
            argument,
            schema);
    }

    public static LogEntry ExternalArgumentDefaultMismatch(
        string argumentName,
        string fieldName,
        string typeName)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);

        return new LogEntry(
            string.Format(LogEntryHelper_ExternalArgumentDefaultMismatch, coordinate),
            LogEntryCodes.ExternalArgumentDefaultMismatch,
            LogSeverity.Error,
            coordinate);
    }

    public static LogEntry ExternalMissingOnBase(
        OutputFieldDefinition externalField,
        INamedTypeDefinition type,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, externalField.Name);

        return new LogEntry(
            string.Format(LogEntryHelper_ExternalMissingOnBase, coordinate, schema.Name),
            LogEntryCodes.ExternalMissingOnBase,
            LogSeverity.Error,
            coordinate,
            externalField,
            schema);
    }

    public static LogEntry OutputFieldTypesNotMergeable(
        OutputFieldDefinition field,
        string typeName,
        SchemaDefinition schemaA,
        SchemaDefinition schemaB)
    {
        var coordinate = new SchemaCoordinate(typeName, field.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_OutputFieldTypesNotMergeable,
                coordinate,
                schemaA.Name,
                schemaB.Name),
            LogEntryCodes.OutputFieldTypesNotMergeable,
            LogSeverity.Error,
            coordinate,
            field,
            schemaA);
    }
}
