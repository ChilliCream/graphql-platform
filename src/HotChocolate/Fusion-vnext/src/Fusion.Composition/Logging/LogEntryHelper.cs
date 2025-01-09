using System.Collections.Immutable;
using HotChocolate.Language;
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

    public static LogEntry EnumValuesMismatch(
        EnumTypeDefinition enumType,
        string enumValue,
        SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_EnumValuesMismatch,
                enumType.Name,
                schema.Name,
                enumValue),
            LogEntryCodes.EnumValuesMismatch,
            LogSeverity.Error,
            new SchemaCoordinate(enumType.Name),
            enumType,
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

    public static LogEntry ExternalOnInterface(
        OutputFieldDefinition externalField,
        INamedTypeDefinition type,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, externalField.Name);

        return new LogEntry(
            string.Format(LogEntryHelper_ExternalOnInterface, coordinate, schema.Name),
            LogEntryCodes.ExternalOnInterface,
            LogSeverity.Error,
            coordinate,
            externalField,
            schema);
    }

    public static LogEntry ExternalUnused(
        OutputFieldDefinition externalField,
        INamedTypeDefinition type,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, externalField.Name);

        return new LogEntry(
            string.Format(LogEntryHelper_ExternalUnused, coordinate, schema.Name),
            LogEntryCodes.ExternalUnused,
            LogSeverity.Error,
            coordinate,
            externalField,
            schema);
    }

    public static LogEntry FieldArgumentTypesNotMergeable(
        InputFieldDefinition argument,
        string fieldName,
        string typeName,
        SchemaDefinition schemaA,
        SchemaDefinition schemaB)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argument.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_FieldArgumentTypesNotMergeable,
                coordinate,
                schemaA.Name,
                schemaB.Name),
            LogEntryCodes.FieldArgumentTypesNotMergeable,
            LogSeverity.Error,
            coordinate,
            argument,
            schemaA);
    }

    public static LogEntry InputFieldDefaultMismatch(
        IValueNode defaultValueA,
        IValueNode defaultValueB,
        InputFieldDefinition field,
        string typeName,
        SchemaDefinition schemaA,
        SchemaDefinition schemaB)
    {
        var coordinate = new SchemaCoordinate(typeName, field.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_InputFieldDefaultMismatch,
                defaultValueA,
                coordinate,
                schemaA.Name,
                defaultValueB,
                schemaB.Name),
            LogEntryCodes.InputFieldDefaultMismatch,
            LogSeverity.Error,
            coordinate,
            field,
            schemaA);
    }

    public static LogEntry InputFieldTypesNotMergeable(
        InputFieldDefinition field,
        string typeName,
        SchemaDefinition schemaA,
        SchemaDefinition schemaB)
    {
        var coordinate = new SchemaCoordinate(typeName, field.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_InputFieldTypesNotMergeable,
                coordinate,
                schemaA.Name,
                schemaB.Name),
            LogEntryCodes.InputFieldTypesNotMergeable,
            LogSeverity.Error,
            coordinate,
            field,
            schemaA);
    }

    public static LogEntry InputWithMissingRequiredFields(
        string requiredFieldName,
        InputObjectTypeDefinition inputType,
        SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_InputWithMissingRequiredFields,
                inputType.Name,
                schema.Name,
                requiredFieldName),
            LogEntryCodes.InputWithMissingRequiredFields,
            LogSeverity.Error,
            new SchemaCoordinate(inputType.Name),
            inputType,
            schema);
    }

    public static LogEntry KeyDirectiveInFieldsArgument(
        string entityTypeName,
        Directive keyDirective,
        ImmutableArray<string> fieldNamePath,
        SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_KeyDirectiveInFieldsArgument,
                entityTypeName,
                schema.Name,
                string.Join(".", fieldNamePath)),
            LogEntryCodes.KeyDirectiveInFieldsArg,
            LogSeverity.Error,
            new SchemaCoordinate(entityTypeName),
            keyDirective,
            schema);
    }

    public static LogEntry KeyFieldsHasArguments(
        string entityTypeName,
        Directive keyDirective,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_KeyFieldsHasArguments,
                entityTypeName,
                schema.Name,
                new SchemaCoordinate(typeName, fieldName)),
            LogEntryCodes.KeyFieldsHasArgs,
            LogSeverity.Error,
            new SchemaCoordinate(entityTypeName),
            keyDirective,
            schema);
    }

    public static LogEntry KeyFieldsSelectInvalidType(
        string entityTypeName,
        Directive keyDirective,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_KeyFieldsSelectInvalidType,
                entityTypeName,
                schema.Name,
                new SchemaCoordinate(typeName, fieldName)),
            LogEntryCodes.KeyFieldsSelectInvalidType,
            LogSeverity.Error,
            new SchemaCoordinate(entityTypeName),
            keyDirective,
            schema);
    }

    public static LogEntry KeyInvalidFields(
        string entityTypeName,
        Directive keyDirective,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_KeyInvalidFields,
                entityTypeName,
                schema.Name,
                new SchemaCoordinate(typeName, fieldName)),
            LogEntryCodes.KeyInvalidFields,
            LogSeverity.Error,
            new SchemaCoordinate(entityTypeName),
            keyDirective,
            schema);
    }

    public static LogEntry KeyInvalidFieldsType(
        Directive keyDirective,
        string entityTypeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(entityTypeName);

        return new LogEntry(
            string.Format(LogEntryHelper_KeyInvalidFieldsType, coordinate, schema.Name),
            LogEntryCodes.KeyInvalidFieldsType,
            LogSeverity.Error,
            coordinate,
            keyDirective,
            schema);
    }

    public static LogEntry KeyInvalidSyntax(
        string entityTypeName,
        Directive keyDirective,
        SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_KeyInvalidSyntax,
                entityTypeName,
                schema.Name),
            LogEntryCodes.KeyInvalidSyntax,
            LogSeverity.Error,
            new SchemaCoordinate(entityTypeName),
            keyDirective,
            schema);
    }

    public static LogEntry LookupMustNotReturnList(
        OutputFieldDefinition field,
        INamedTypeDefinition type,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, field.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_LookupMustNotReturnList,
                coordinate,
                schema.Name),
            LogEntryCodes.LookupMustNotReturnList,
            LogSeverity.Error,
            coordinate,
            field,
            schema);
    }

    public static LogEntry LookupShouldHaveNullableReturnType(
        OutputFieldDefinition field,
        INamedTypeDefinition type,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, field.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_LookupShouldHaveNullableReturnType,
                coordinate,
                schema.Name),
            LogEntryCodes.LookupShouldHaveNullableReturnType,
            LogSeverity.Warning,
            coordinate,
            field,
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

    public static LogEntry OverrideFromSelf(
        Directive overrideDirective,
        OutputFieldDefinition field,
        INamedTypeDefinition type,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, field.Name);

        return new LogEntry(
            string.Format(LogEntryHelper_OverrideFromSelf, coordinate, schema.Name),
            LogEntryCodes.OverrideFromSelf,
            LogSeverity.Error,
            coordinate,
            overrideDirective,
            schema);
    }

    public static LogEntry OverrideOnInterface(
        OutputFieldDefinition field,
        INamedTypeDefinition type,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, field.Name);

        return new LogEntry(
            string.Format(LogEntryHelper_OverrideOnInterface, coordinate, schema.Name),
            LogEntryCodes.OverrideOnInterface,
            LogSeverity.Error,
            coordinate,
            field,
            schema);
    }

    public static LogEntry ProvidesDirectiveInFieldsArgument(
        ImmutableArray<string> fieldNamePath,
        Directive providesDirective,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName);

        return new LogEntry(
            string.Format(
                LogEntryHelper_ProvidesDirectiveInFieldsArgument,
                coordinate,
                schema.Name,
                string.Join(".", fieldNamePath)),
            LogEntryCodes.ProvidesDirectiveInFieldsArg,
            LogSeverity.Error,
            coordinate,
            providesDirective,
            schema);
    }

    public static LogEntry ProvidesFieldsHasArguments(
        string providedFieldName,
        string providedTypeName,
        Directive providesDirective,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName);

        return new LogEntry(
            string.Format(
                LogEntryHelper_ProvidesFieldsHasArguments,
                coordinate,
                schema.Name,
                new SchemaCoordinate(providedTypeName, providedFieldName)),
            LogEntryCodes.ProvidesFieldsHasArgs,
            LogSeverity.Error,
            coordinate,
            providesDirective,
            schema);
    }

    public static LogEntry ProvidesFieldsMissingExternal(
        string providedFieldName,
        string providedTypeName,
        Directive providesDirective,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName);

        return new LogEntry(
            string.Format(
                LogEntryHelper_ProvidesFieldsMissingExternal,
                coordinate,
                schema.Name,
                new SchemaCoordinate(providedTypeName, providedFieldName)),
            LogEntryCodes.ProvidesFieldsMissingExternal,
            LogSeverity.Error,
            coordinate,
            providesDirective,
            schema);
    }

    public static LogEntry ProvidesInvalidFieldsType(
        Directive providesDirective,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName);

        return new LogEntry(
            string.Format(LogEntryHelper_ProvidesInvalidFieldsType, coordinate, schema.Name),
            LogEntryCodes.ProvidesInvalidFieldsType,
            LogSeverity.Error,
            coordinate,
            providesDirective,
            schema);
    }

    public static LogEntry ProvidesInvalidSyntax(
        Directive providesDirective,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName);

        return new LogEntry(
            string.Format(LogEntryHelper_ProvidesInvalidSyntax, coordinate, schema.Name),
            LogEntryCodes.ProvidesInvalidSyntax,
            LogSeverity.Error,
            coordinate,
            providesDirective,
            schema);
    }

    public static LogEntry ProvidesOnNonCompositeField(
        OutputFieldDefinition field,
        INamedTypeDefinition type,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, field.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_ProvidesOnNonCompositeField,
                coordinate,
                schema.Name),
            LogEntryCodes.ProvidesOnNonCompositeField,
            LogSeverity.Error,
            coordinate,
            field,
            schema);
    }

    public static LogEntry QueryRootTypeInaccessible(
        INamedTypeDefinition type,
        SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_QueryRootTypeInaccessible, schema.Name),
            LogEntryCodes.QueryRootTypeInaccessible,
            LogSeverity.Error,
            new SchemaCoordinate(type.Name),
            type,
            schema);
    }

    public static LogEntry RequireDirectiveInFieldsArgument(
        ImmutableArray<string> fieldNamePath,
        Directive requireDirective,
        string argumentName,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);

        return new LogEntry(
            string.Format(
                LogEntryHelper_RequireDirectiveInFieldsArgument,
                coordinate,
                schema.Name,
                string.Join(".", fieldNamePath)),
            LogEntryCodes.RequireDirectiveInFieldsArg,
            LogSeverity.Error,
            coordinate,
            requireDirective,
            schema);
    }

    public static LogEntry RequireInvalidFieldsType(
        Directive requireDirective,
        string argumentName,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);

        return new LogEntry(
            string.Format(LogEntryHelper_RequireInvalidFieldsType, coordinate, schema.Name),
            LogEntryCodes.RequireInvalidFieldsType,
            LogSeverity.Error,
            coordinate,
            requireDirective,
            schema);
    }

    public static LogEntry RequireInvalidSyntax(
        Directive requireDirective,
        string argumentName,
        string fieldName,
        string typeName,
        SchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);

        return new LogEntry(
            string.Format(LogEntryHelper_RequireInvalidSyntax, coordinate, schema.Name),
            LogEntryCodes.RequireInvalidSyntax,
            LogSeverity.Error,
            coordinate,
            requireDirective,
            schema);
    }

    public static LogEntry RootMutationUsed(SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_RootMutationUsed, schema.Name),
            LogEntryCodes.RootMutationUsed,
            severity: LogSeverity.Error,
            member: schema,
            schema: schema);
    }

    public static LogEntry RootQueryUsed(SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_RootQueryUsed, schema.Name),
            LogEntryCodes.RootQueryUsed,
            severity: LogSeverity.Error,
            member: schema,
            schema: schema);
    }

    public static LogEntry RootSubscriptionUsed(SchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_RootSubscriptionUsed, schema.Name),
            LogEntryCodes.RootSubscriptionUsed,
            severity: LogSeverity.Error,
            member: schema,
            schema: schema);
    }
}
