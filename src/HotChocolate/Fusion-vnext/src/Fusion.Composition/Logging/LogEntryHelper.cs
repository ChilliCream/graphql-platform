using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Logging;

internal static class LogEntryHelper
{
    public static LogEntry DisallowedInaccessibleBuiltInScalar(
        MutableScalarTypeDefinition scalar,
        MutableSchemaDefinition schema)
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
        ITypeDefinition type,
        MutableSchemaDefinition schema)
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
        MutableOutputFieldDefinition field,
        string typeName,
        MutableSchemaDefinition schema)
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
        MutableInputFieldDefinition argument,
        string fieldName,
        string typeName,
        MutableSchemaDefinition schema)
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
        MutableInputFieldDefinition argument,
        string directiveName,
        MutableSchemaDefinition schema)
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

    public static LogEntry EmptyMergedEnumType(
        MutableEnumTypeDefinition enumType,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_EmptyMergedEnumType, enumType.Name),
            LogEntryCodes.EmptyMergedEnumType,
            LogSeverity.Error,
            new SchemaCoordinate(enumType.Name),
            enumType,
            schema);
    }

    public static LogEntry EmptyMergedInputObjectType(
        MutableInputObjectTypeDefinition inputObjectType,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_EmptyMergedInputObjectType, inputObjectType.Name),
            LogEntryCodes.EmptyMergedInputObjectType,
            LogSeverity.Error,
            new SchemaCoordinate(inputObjectType.Name),
            inputObjectType,
            schema);
    }

    public static LogEntry EmptyMergedInterfaceType(
        MutableInterfaceTypeDefinition interfaceType,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_EmptyMergedInterfaceType, interfaceType.Name),
            LogEntryCodes.EmptyMergedInterfaceType,
            LogSeverity.Error,
            new SchemaCoordinate(interfaceType.Name),
            interfaceType,
            schema);
    }

    public static LogEntry EmptyMergedObjectType(
        MutableObjectTypeDefinition objectType,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_EmptyMergedObjectType, objectType.Name),
            LogEntryCodes.EmptyMergedObjectType,
            LogSeverity.Error,
            new SchemaCoordinate(objectType.Name),
            objectType,
            schema);
    }

    public static LogEntry EmptyMergedUnionType(
        MutableUnionTypeDefinition unionType,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_EmptyMergedUnionType, unionType.Name),
            LogEntryCodes.EmptyMergedUnionType,
            LogSeverity.Error,
            new SchemaCoordinate(unionType.Name),
            unionType,
            schema);
    }

    public static LogEntry EnumTypeDefaultValueInaccessible(
        SchemaCoordinate coordinate,
        SchemaCoordinate inaccessibleCoordinate,
        ITypeSystemMember type,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_EnumTypeDefaultValueInaccessible,
                coordinate,
                inaccessibleCoordinate),
            LogEntryCodes.EnumTypeDefaultValueInaccessible,
            LogSeverity.Error,
            coordinate,
            type,
            schema);
    }

    public static LogEntry EnumValuesMismatch(
        MutableEnumTypeDefinition enumType,
        string enumValue,
        MutableSchemaDefinition schema)
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
        IValueNode? externalDefaultValue,
        MutableInputFieldDefinition externalArgument,
        string fieldName,
        string typeName,
        MutableSchemaDefinition externalSchema,
        IValueNode? defaultValue,
        string schemaName)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, externalArgument.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_ExternalArgumentDefaultMismatch,
                externalDefaultValue is null ? "(null)" : externalDefaultValue.ToString(),
                coordinate,
                externalSchema.Name,
                defaultValue is null ? "(null)" : defaultValue.ToString(),
                schemaName),
            LogEntryCodes.ExternalArgumentDefaultMismatch,
            LogSeverity.Error,
            coordinate,
            externalArgument,
            externalSchema);
    }

    public static LogEntry ExternalMissingOnBase(
        MutableOutputFieldDefinition externalField,
        ITypeDefinition type,
        MutableSchemaDefinition schema)
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
        MutableOutputFieldDefinition externalField,
        ITypeDefinition type,
        MutableSchemaDefinition schema)
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
        MutableOutputFieldDefinition externalField,
        ITypeDefinition type,
        MutableSchemaDefinition schema)
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
        MutableInputFieldDefinition argument,
        string fieldName,
        string typeName,
        MutableSchemaDefinition schemaA,
        MutableSchemaDefinition schemaB)
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

    public static LogEntry FieldWithMissingRequiredArgument(
        string requiredArgumentName,
        MutableOutputFieldDefinition field,
        string typeName,
        MutableSchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, field.Name, requiredArgumentName);

        return new LogEntry(
            string.Format(LogEntryHelper_FieldWithMissingRequiredArgument, coordinate, schema.Name),
            LogEntryCodes.FieldWithMissingRequiredArgument,
            LogSeverity.Error,
            coordinate,
            field,
            schema);
    }

    public static LogEntry InputFieldDefaultMismatch(
        IValueNode defaultValueA,
        IValueNode defaultValueB,
        MutableInputFieldDefinition field,
        string typeName,
        MutableSchemaDefinition schemaA,
        MutableSchemaDefinition schemaB)
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
        MutableInputFieldDefinition field,
        string typeName,
        MutableSchemaDefinition schemaA,
        MutableSchemaDefinition schemaB)
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
        MutableInputObjectTypeDefinition inputType,
        MutableSchemaDefinition schema)
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

    public static LogEntry InterfaceFieldNoImplementation(
        MutableObjectTypeDefinition objectType,
        string fieldName,
        string interfaceName,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_InterfaceFieldNoImplementation,
                objectType.Name,
                fieldName,
                interfaceName),
            LogEntryCodes.InterfaceFieldNoImplementation,
            LogSeverity.Error,
            new SchemaCoordinate(objectType.Name),
            objectType,
            schema);
    }

    public static LogEntry InvalidGraphQL(string exceptionMessage)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_InvalidGraphQL, exceptionMessage),
            LogEntryCodes.InvalidGraphQL,
            severity: LogSeverity.Error);
    }

    public static LogEntry InvalidShareableUsage(
        MutableOutputFieldDefinition field,
        ITypeDefinition type,
        MutableSchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, field.Name);

        return new LogEntry(
            string.Format(LogEntryHelper_InvalidShareableUsage, coordinate, schema.Name),
            LogEntryCodes.InvalidShareableUsage,
            LogSeverity.Error,
            coordinate,
            field,
            schema);
    }

    public static LogEntry IsInvalidField(
        Directive isDirective,
        string argumentName,
        string fieldName,
        string typeName,
        MutableSchemaDefinition sourceSchema,
        ImmutableArray<string> errors)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);

        return new LogEntry(
            string.Format(LogEntryHelper_IsInvalidField, coordinate, sourceSchema.Name),
            LogEntryCodes.IsInvalidField,
            LogSeverity.Error,
            coordinate,
            isDirective,
            sourceSchema,
            errors);
    }

    public static LogEntry IsInvalidFieldType(
        Directive isDirective,
        string argumentName,
        string fieldName,
        string typeName,
        MutableSchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);

        return new LogEntry(
            string.Format(LogEntryHelper_IsInvalidFieldType, coordinate, schema.Name),
            LogEntryCodes.IsInvalidFieldType,
            LogSeverity.Error,
            coordinate,
            isDirective,
            schema);
    }

    public static LogEntry IsInvalidSyntax(
        Directive isDirective,
        string argumentName,
        string fieldName,
        string typeName,
        MutableSchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);

        return new LogEntry(
            string.Format(LogEntryHelper_IsInvalidSyntax, coordinate, schema.Name),
            LogEntryCodes.IsInvalidSyntax,
            LogSeverity.Error,
            coordinate,
            isDirective,
            schema);
    }

    public static LogEntry IsInvalidUsage(
        Directive isDirective,
        string argumentName,
        string fieldName,
        string typeName,
        MutableSchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);

        return new LogEntry(
            string.Format(LogEntryHelper_IsInvalidUsage, coordinate, schema.Name),
            LogEntryCodes.IsInvalidUsage,
            LogSeverity.Error,
            coordinate,
            isDirective,
            schema);
    }

    public static LogEntry KeyDirectiveInFieldsArgument(
        string typeName,
        Directive keyDirective,
        ImmutableArray<string> fieldNamePath,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_KeyDirectiveInFieldsArgument,
                typeName,
                schema.Name,
                string.Join(".", fieldNamePath)),
            LogEntryCodes.KeyDirectiveInFieldsArg,
            LogSeverity.Error,
            new SchemaCoordinate(typeName),
            keyDirective,
            schema);
    }

    public static LogEntry KeyFieldsHasArguments(
        string keyFieldName,
        string keyFieldDeclaringTypeName,
        Directive keyDirective,
        string typeName,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_KeyFieldsHasArguments,
                typeName,
                schema.Name,
                new SchemaCoordinate(keyFieldDeclaringTypeName, keyFieldName)),
            LogEntryCodes.KeyFieldsHasArgs,
            LogSeverity.Error,
            new SchemaCoordinate(typeName),
            keyDirective,
            schema);
    }

    public static LogEntry KeyFieldsSelectInvalidType(
        string keyFieldName,
        string keyFieldDeclaringTypeName,
        Directive keyDirective,
        string typeName,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_KeyFieldsSelectInvalidType,
                typeName,
                schema.Name,
                new SchemaCoordinate(keyFieldDeclaringTypeName, keyFieldName)),
            LogEntryCodes.KeyFieldsSelectInvalidType,
            LogSeverity.Error,
            new SchemaCoordinate(typeName),
            keyDirective,
            schema);
    }

    public static LogEntry KeyInvalidFields(
        Directive keyDirective,
        string typeName,
        MutableSchemaDefinition schema,
        ImmutableArray<string> errors)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_KeyInvalidFields, typeName, schema.Name),
            LogEntryCodes.KeyInvalidFields,
            LogSeverity.Error,
            new SchemaCoordinate(typeName),
            keyDirective,
            schema,
            errors);
    }

    public static LogEntry KeyInvalidFieldsType(
        Directive keyDirective,
        string typeName,
        MutableSchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName);

        return new LogEntry(
            string.Format(LogEntryHelper_KeyInvalidFieldsType, coordinate, schema.Name),
            LogEntryCodes.KeyInvalidFieldsType,
            LogSeverity.Error,
            coordinate,
            keyDirective,
            schema);
    }

    public static LogEntry KeyInvalidSyntax(
        string typeName,
        Directive keyDirective,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_KeyInvalidSyntax,
                typeName,
                schema.Name),
            LogEntryCodes.KeyInvalidSyntax,
            LogSeverity.Error,
            new SchemaCoordinate(typeName),
            keyDirective,
            schema);
    }

    public static LogEntry LookupReturnsList(
        MutableOutputFieldDefinition field,
        ITypeDefinition type,
        MutableSchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, field.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_LookupReturnsList,
                coordinate,
                schema.Name),
            LogEntryCodes.LookupReturnsList,
            LogSeverity.Error,
            coordinate,
            field,
            schema);
    }

    public static LogEntry LookupReturnsNonNullableType(
        MutableOutputFieldDefinition field,
        ITypeDefinition type,
        MutableSchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(type.Name, field.Name);

        return new LogEntry(
            string.Format(
                LogEntryHelper_LookupReturnsNonNullableType,
                coordinate,
                schema.Name),
            LogEntryCodes.LookupReturnsNonNullableType,
            LogSeverity.Warning,
            coordinate,
            field,
            schema);
    }

    public static LogEntry NonNullInputFieldIsInaccessible(
        MutableInputFieldDefinition inputField,
        SchemaCoordinate coordinate,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_NonNullInputFieldIsInaccessible, coordinate, schema.Name),
            LogEntryCodes.NonNullInputFieldIsInaccessible,
            LogSeverity.Error,
            coordinate,
            inputField,
            schema);
    }

    public static LogEntry NoQueries(MutableObjectTypeDefinition queryType, MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_NoQueries),
            LogEntryCodes.NoQueries,
            LogSeverity.Error,
            new SchemaCoordinate(queryType.Name),
            queryType,
            schema);
    }

    public static LogEntry OutputFieldTypesNotMergeable(
        MutableOutputFieldDefinition field,
        string typeName,
        MutableSchemaDefinition schemaA,
        MutableSchemaDefinition schemaB)
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
        MutableOutputFieldDefinition field,
        ITypeDefinition type,
        MutableSchemaDefinition schema)
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
        MutableOutputFieldDefinition field,
        ITypeDefinition type,
        MutableSchemaDefinition schema)
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
        MutableSchemaDefinition schema)
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
        MutableSchemaDefinition schema)
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
        MutableSchemaDefinition schema)
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

    public static LogEntry ProvidesInvalidFields(
        Directive providesDirective,
        string fieldName,
        string typeName,
        MutableSchemaDefinition schema,
        ImmutableArray<string> errors)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName);

        return new LogEntry(
            string.Format(LogEntryHelper_ProvidesInvalidFields, coordinate, schema.Name),
            LogEntryCodes.ProvidesInvalidFields,
            LogSeverity.Error,
            coordinate,
            providesDirective,
            schema,
            errors);
    }

    public static LogEntry ProvidesInvalidFieldsType(
        Directive providesDirective,
        string fieldName,
        string typeName,
        MutableSchemaDefinition schema)
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
        MutableSchemaDefinition schema)
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
        MutableOutputFieldDefinition field,
        ITypeDefinition type,
        MutableSchemaDefinition schema)
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
        ITypeDefinition type,
        MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_QueryRootTypeInaccessible, schema.Name),
            LogEntryCodes.QueryRootTypeInaccessible,
            LogSeverity.Error,
            new SchemaCoordinate(type.Name),
            type,
            schema);
    }

    public static LogEntry RequireInvalidFields(
        Directive fusionRequiresDirective,
        string argumentName,
        string fieldName,
        string typeName,
        string sourceSchemaName,
        MutableSchemaDefinition schema,
        ImmutableArray<string> errors)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);

        return new LogEntry(
            string.Format(LogEntryHelper_RequireInvalidFields, coordinate, sourceSchemaName),
            LogEntryCodes.RequireInvalidFields,
            LogSeverity.Error,
            coordinate,
            fusionRequiresDirective,
            schema,
            errors);
    }

    public static LogEntry RequireInvalidFieldType(
        Directive requireDirective,
        string argumentName,
        string fieldName,
        string typeName,
        MutableSchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, fieldName, argumentName);

        return new LogEntry(
            string.Format(LogEntryHelper_RequireInvalidFieldType, coordinate, schema.Name),
            LogEntryCodes.RequireInvalidFieldType,
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
        MutableSchemaDefinition schema)
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

    public static LogEntry RootMutationUsed(MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_RootMutationUsed, schema.Name),
            LogEntryCodes.RootMutationUsed,
            severity: LogSeverity.Error,
            member: schema,
            schema: schema);
    }

    public static LogEntry RootQueryUsed(MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_RootQueryUsed, schema.Name),
            LogEntryCodes.RootQueryUsed,
            severity: LogSeverity.Error,
            member: schema,
            schema: schema);
    }

    public static LogEntry RootSubscriptionUsed(MutableSchemaDefinition schema)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_RootSubscriptionUsed, schema.Name),
            LogEntryCodes.RootSubscriptionUsed,
            severity: LogSeverity.Error,
            member: schema,
            schema: schema);
    }

    public static LogEntry TypeDefinitionInvalid(
        INameProvider member,
        MutableSchemaDefinition schema,
        string? details = null)
    {
        return new LogEntry(
            string.Format(LogEntryHelper_TypeDefinitionInvalid, member.Name, schema.Name),
            LogEntryCodes.TypeDefinitionInvalid,
            LogSeverity.Error,
            new SchemaCoordinate(member.Name),
            member,
            schema,
            details);
    }

    public static LogEntry TypeKindMismatch(
        ITypeDefinition type,
        MutableSchemaDefinition schemaA,
        string typeKindA,
        MutableSchemaDefinition schemaB,
        string typeKindB)
    {
        return new LogEntry(
            string.Format(
                LogEntryHelper_TypeKindMismatch,
                type.Name,
                schemaA.Name,
                typeKindA,
                schemaB.Name,
                typeKindB),
            LogEntryCodes.TypeKindMismatch,
            LogSeverity.Error,
            new SchemaCoordinate(type.Name),
            type,
            schemaA);
    }
}
