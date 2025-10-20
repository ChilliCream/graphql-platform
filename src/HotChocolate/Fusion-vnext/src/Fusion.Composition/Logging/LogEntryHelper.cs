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
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_DisallowedInaccessibleBuiltInScalar,
                scalar.Name,
                schema.Name)
            .SetCode(LogEntryCodes.DisallowedInaccessible)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(scalar)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry DisallowedInaccessibleIntrospectionType(
        ITypeDefinition type,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_DisallowedInaccessibleIntrospectionType,
                type.Name,
                schema.Name)
            .SetCode(LogEntryCodes.DisallowedInaccessible)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(type)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry DisallowedInaccessibleIntrospectionField(
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_DisallowedInaccessibleIntrospectionField,
                field.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.DisallowedInaccessible)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry DisallowedInaccessibleIntrospectionArgument(
        MutableInputFieldDefinition argument,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_DisallowedInaccessibleIntrospectionArgument,
                argument.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.DisallowedInaccessible)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(argument)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry DisallowedInaccessibleDirectiveArgument(
        MutableInputFieldDefinition argument,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_DisallowedInaccessibleDirectiveArgument,
                argument.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.DisallowedInaccessible)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(argument)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry EmptyMergedEnumType(
        MutableEnumTypeDefinition enumType,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_EmptyMergedEnumType, enumType.Name)
            .SetCode(LogEntryCodes.EmptyMergedEnumType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(enumType)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry EmptyMergedInputObjectType(
        MutableInputObjectTypeDefinition inputObjectType,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_EmptyMergedInputObjectType, inputObjectType.Name)
            .SetCode(LogEntryCodes.EmptyMergedInputObjectType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(inputObjectType)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry EmptyMergedInterfaceType(
        MutableInterfaceTypeDefinition interfaceType,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_EmptyMergedInterfaceType, interfaceType.Name)
            .SetCode(LogEntryCodes.EmptyMergedInterfaceType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(interfaceType)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry EmptyMergedObjectType(
        MutableObjectTypeDefinition objectType,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_EmptyMergedObjectType, objectType.Name)
            .SetCode(LogEntryCodes.EmptyMergedObjectType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(objectType)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry EmptyMergedUnionType(
        MutableUnionTypeDefinition unionType,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_EmptyMergedUnionType, unionType.Name)
            .SetCode(LogEntryCodes.EmptyMergedUnionType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(unionType)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry EnumTypeDefaultValueInaccessible(
        MutableInputFieldDefinition inputField,
        SchemaCoordinate inaccessibleCoordinate,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_EnumTypeDefaultValueInaccessible,
                inputField.Coordinate.ToString(),
                inaccessibleCoordinate.ToString())
            .SetCode(LogEntryCodes.EnumTypeDefaultValueInaccessible)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(inputField)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry EnumValuesMismatch(
        MutableEnumTypeDefinition enumType,
        string enumValue,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_EnumValuesMismatch,
                enumType.Name,
                schema.Name,
                enumValue)
            .SetCode(LogEntryCodes.EnumValuesMismatch)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(enumType)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ExternalArgumentDefaultMismatch(
        IValueNode? externalDefaultValue,
        MutableInputFieldDefinition externalArgument,
        MutableSchemaDefinition externalSchema,
        IValueNode? defaultValue,
        string schemaName)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ExternalArgumentDefaultMismatch,
                externalDefaultValue is null ? "(null)" : externalDefaultValue.ToString(),
                externalArgument.Coordinate.ToString(),
                externalSchema.Name,
                defaultValue is null ? "(null)" : defaultValue.ToString(),
                schemaName)
            .SetCode(LogEntryCodes.ExternalArgumentDefaultMismatch)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(externalArgument)
            .SetSchema(externalSchema)
            .Build();
    }

    public static LogEntry ExternalMissingOnBase(
        MutableOutputFieldDefinition externalField,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ExternalMissingOnBase,
                externalField.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.ExternalMissingOnBase)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(externalField)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ExternalOnInterface(
        MutableOutputFieldDefinition externalField,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ExternalOnInterface,
                externalField.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.ExternalOnInterface)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(externalField)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ExternalOverrideCollision(
        MutableOutputFieldDefinition externalField,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ExternalOverrideCollision,
                externalField.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.ExternalOverrideCollision)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(externalField)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ExternalProvidesCollision(
        MutableOutputFieldDefinition externalField,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ExternalProvidesCollision,
                externalField.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.ExternalProvidesCollision)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(externalField)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ExternalRequireCollision(
        MutableOutputFieldDefinition externalField,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ExternalRequireCollision,
                externalField.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.ExternalRequireCollision)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(externalField)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ExternalUnused(
        MutableOutputFieldDefinition externalField,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ExternalUnused,
                externalField.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.ExternalUnused)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(externalField)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry FieldArgumentTypesNotMergeable(
        MutableInputFieldDefinition argument,
        MutableSchemaDefinition schemaA,
        MutableSchemaDefinition schemaB)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_FieldArgumentTypesNotMergeable,
                argument.Coordinate.ToString(),
                schemaA.Name,
                schemaB.Name)
            .SetCode(LogEntryCodes.FieldArgumentTypesNotMergeable)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(argument)
            .SetSchema(schemaA)
            .Build();
    }

    public static LogEntry FieldWithMissingRequiredArgument(
        string requiredArgumentName,
        MutableOutputFieldDefinition field,
        string typeName,
        MutableSchemaDefinition schema)
    {
        var coordinate = new SchemaCoordinate(typeName, field.Name, requiredArgumentName);

        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_FieldWithMissingRequiredArgument,
                coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.FieldWithMissingRequiredArgument)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(field)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ImplementedByInaccessible(
        MutableOutputFieldDefinition field,
        string interfaceFieldName,
        string interfaceTypeName,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ImplementedByInaccessible,
                field.Coordinate.ToString(),
                new SchemaCoordinate(interfaceTypeName, interfaceFieldName).ToString())
            .SetCode(LogEntryCodes.ImplementedByInaccessible)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry InputFieldDefaultMismatch(
        IValueNode defaultValueA,
        IValueNode defaultValueB,
        MutableInputFieldDefinition field,
        MutableSchemaDefinition schemaA,
        MutableSchemaDefinition schemaB)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_InputFieldDefaultMismatch,
                defaultValueA,
                field.Coordinate.ToString(),
                schemaA.Name,
                defaultValueB,
                schemaB.Name)
            .SetCode(LogEntryCodes.InputFieldDefaultMismatch)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetSchema(schemaA)
            .Build();
    }

    public static LogEntry InputFieldTypesNotMergeable(
        MutableInputFieldDefinition field,
        MutableSchemaDefinition schemaA,
        MutableSchemaDefinition schemaB)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_InputFieldTypesNotMergeable,
                field.Coordinate.ToString(),
                schemaA.Name,
                schemaB.Name)
            .SetCode(LogEntryCodes.InputFieldTypesNotMergeable)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetSchema(schemaA)
            .Build();
    }

    public static LogEntry InputWithMissingRequiredFields(
        string requiredFieldName,
        MutableInputObjectTypeDefinition inputType,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_InputWithMissingRequiredFields,
                inputType.Name,
                schema.Name,
                requiredFieldName)
            .SetCode(LogEntryCodes.InputWithMissingRequiredFields)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(inputType)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry InterfaceFieldNoImplementation(
        MutableObjectTypeDefinition objectType,
        string fieldName,
        string interfaceName,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_InterfaceFieldNoImplementation,
                objectType.Name,
                fieldName,
                interfaceName)
            .SetCode(LogEntryCodes.InterfaceFieldNoImplementation)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(objectType)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry InvalidFieldSharing(
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_InvalidFieldSharing,
                field.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.InvalidFieldSharing)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry InvalidGraphQL(string exceptionMessage, MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_InvalidGraphQL, exceptionMessage)
            .SetCode(LogEntryCodes.InvalidGraphQL)
            .SetSeverity(LogSeverity.Error)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry InvalidShareableUsage(
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_InvalidShareableUsage,
                field.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.InvalidShareableUsage)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry IsInvalidFields(
        Directive isDirective,
        MutableInputFieldDefinition argument,
        MutableSchemaDefinition sourceSchema,
        ImmutableArray<string> errors)
    {
        var coordinate = argument.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_IsInvalidFields, coordinate.ToString(), sourceSchema.Name)
            .SetCode(LogEntryCodes.IsInvalidFields)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(isDirective)
            .SetSchema(sourceSchema)
            .SetExtension("errors", errors)
            .SetExtensionsFormatter(ErrorFormatter)
            .Build();
    }

    public static LogEntry IsInvalidFieldType(
        Directive isDirective,
        MutableInputFieldDefinition argument,
        MutableSchemaDefinition schema)
    {
        var coordinate = argument.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_IsInvalidFieldType, coordinate.ToString(), schema.Name)
            .SetCode(LogEntryCodes.IsInvalidFieldType)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(isDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry IsInvalidSyntax(
        Directive isDirective,
        MutableInputFieldDefinition argument,
        MutableSchemaDefinition schema)
    {
        var coordinate = argument.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_IsInvalidSyntax, coordinate.ToString(), schema.Name)
            .SetCode(LogEntryCodes.IsInvalidSyntax)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(isDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry IsInvalidUsage(
        Directive isDirective,
        MutableInputFieldDefinition argument,
        MutableSchemaDefinition schema)
    {
        var coordinate = argument.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_IsInvalidUsage, coordinate.ToString(), schema.Name)
            .SetCode(LogEntryCodes.IsInvalidUsage)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(isDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry KeyDirectiveInFieldsArgument(
        MutableComplexTypeDefinition type,
        Directive keyDirective,
        ImmutableArray<string> fieldNamePath,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_KeyDirectiveInFieldsArgument,
                type.Name,
                schema.Name,
                string.Join(".", fieldNamePath))
            .SetCode(LogEntryCodes.KeyDirectiveInFieldsArg)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(type.Coordinate)
            .SetTypeSystemMember(keyDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry KeyFieldsHasArguments(
        string keyFieldName,
        string keyFieldDeclaringTypeName,
        Directive keyDirective,
        MutableComplexTypeDefinition type,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_KeyFieldsHasArguments,
                type.Name,
                schema.Name,
                new SchemaCoordinate(keyFieldDeclaringTypeName, keyFieldName).ToString())
            .SetCode(LogEntryCodes.KeyFieldsHasArgs)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(type.Coordinate)
            .SetTypeSystemMember(keyDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry KeyFieldsSelectInvalidType(
        string keyFieldName,
        string keyFieldDeclaringTypeName,
        Directive keyDirective,
        MutableComplexTypeDefinition type,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_KeyFieldsSelectInvalidType,
                type.Name,
                schema.Name,
                new SchemaCoordinate(keyFieldDeclaringTypeName, keyFieldName).ToString())
            .SetCode(LogEntryCodes.KeyFieldsSelectInvalidType)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(type.Coordinate)
            .SetTypeSystemMember(keyDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry KeyInvalidFields(
        Directive keyDirective,
        MutableComplexTypeDefinition type,
        MutableSchemaDefinition schema,
        ImmutableArray<string> errors)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_KeyInvalidFields, type.Name, schema.Name)
            .SetCode(LogEntryCodes.KeyInvalidFields)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(type.Coordinate)
            .SetTypeSystemMember(keyDirective)
            .SetSchema(schema)
            .SetExtension("errors", errors)
            .SetExtensionsFormatter(ErrorFormatter)
            .Build();
    }

    public static LogEntry KeyInvalidFieldsType(
        Directive keyDirective,
        MutableComplexTypeDefinition type,
        MutableSchemaDefinition schema)
    {
        var coordinate = type.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_KeyInvalidFieldsType, coordinate.ToString(), schema.Name)
            .SetCode(LogEntryCodes.KeyInvalidFieldsType)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(keyDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry KeyInvalidSyntax(
        MutableComplexTypeDefinition type,
        Directive keyDirective,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_KeyInvalidSyntax,
                type.Name,
                schema.Name)
            .SetCode(LogEntryCodes.KeyInvalidSyntax)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(type.Coordinate)
            .SetTypeSystemMember(keyDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry LookupReturnsList(
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_LookupReturnsList,
                field.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.LookupReturnsList)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry LookupReturnsNonNullableType(
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_LookupReturnsNonNullableType,
                field.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.LookupReturnsNonNullableType)
            .SetSeverity(LogSeverity.Warning)
            .SetTypeSystemMember(field)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry NonNullInputFieldIsInaccessible(
        MutableInputFieldDefinition inputField,
        SchemaCoordinate coordinate,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_NonNullInputFieldIsInaccessible,
                coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.NonNullInputFieldIsInaccessible)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(inputField)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry NoQueries(
        MutableObjectTypeDefinition queryType,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_NoQueries)
            .SetCode(LogEntryCodes.NoQueries)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(queryType)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry OutputFieldTypesNotMergeable(
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schemaA,
        MutableSchemaDefinition schemaB)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_OutputFieldTypesNotMergeable,
                field.Coordinate,
                schemaA.Name,
                schemaB.Name)
            .SetCode(LogEntryCodes.OutputFieldTypesNotMergeable)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetSchema(schemaA)
            .Build();
    }

    public static LogEntry OverrideFromSelf(
        Directive overrideDirective,
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        var coordinate = field.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_OverrideFromSelf, coordinate.ToString(), schema.Name)
            .SetCode(LogEntryCodes.OverrideFromSelf)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(overrideDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry OverrideOnInterface(
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_OverrideOnInterface,
                field.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.OverrideOnInterface)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ProvidesDirectiveInFieldsArgument(
        ImmutableArray<string> fieldNamePath,
        Directive providesDirective,
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        var coordinate = field.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ProvidesDirectiveInFieldsArgument,
                coordinate.ToString(),
                schema.Name,
                string.Join(".", fieldNamePath))
            .SetCode(LogEntryCodes.ProvidesDirectiveInFieldsArg)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(providesDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ProvidesFieldsHasArguments(
        string providedFieldName,
        string providedTypeName,
        Directive providesDirective,
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        var coordinate = field.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ProvidesFieldsHasArguments,
                coordinate.ToString(),
                schema.Name,
                new SchemaCoordinate(providedTypeName, providedFieldName).ToString())
            .SetCode(LogEntryCodes.ProvidesFieldsHasArgs)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(providesDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ProvidesFieldsMissingExternal(
        string providedFieldName,
        string providedTypeName,
        Directive providesDirective,
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        var coordinate = field.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ProvidesFieldsMissingExternal,
                coordinate.ToString(),
                schema.Name,
                new SchemaCoordinate(providedTypeName, providedFieldName).ToString())
            .SetCode(LogEntryCodes.ProvidesFieldsMissingExternal)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(providesDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ProvidesInvalidFields(
        Directive providesDirective,
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema,
        ImmutableArray<string> errors)
    {
        var coordinate = field.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_ProvidesInvalidFields, coordinate.ToString(), schema.Name)
            .SetCode(LogEntryCodes.ProvidesInvalidFields)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(providesDirective)
            .SetSchema(schema)
            .SetExtension("errors", errors)
            .SetExtensionsFormatter(ErrorFormatter)
            .Build();
    }

    public static LogEntry ProvidesInvalidFieldsType(
        Directive providesDirective,
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        var coordinate = field.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ProvidesInvalidFieldsType,
                coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.ProvidesInvalidFieldsType)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(providesDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ProvidesInvalidSyntax(
        Directive providesDirective,
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        var coordinate = field.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_ProvidesInvalidSyntax, coordinate.ToString(), schema.Name)
            .SetCode(LogEntryCodes.ProvidesInvalidSyntax)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(providesDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry ProvidesOnNonCompositeField(
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ProvidesOnNonCompositeField,
                field.Coordinate.ToString(),
                schema.Name)
            .SetCode(LogEntryCodes.ProvidesOnNonCompositeField)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry QueryRootTypeInaccessible(
        ITypeDefinition type,
        MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_QueryRootTypeInaccessible, schema.Name)
            .SetCode(LogEntryCodes.QueryRootTypeInaccessible)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(type)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry RequireInvalidFields(
        Directive requireDirective,
        MutableInputFieldDefinition argument,
        MutableSchemaDefinition sourceSchema,
        ImmutableArray<string> errors)
    {
        var coordinate = argument.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_RequireInvalidFields,
                coordinate.ToString(),
                sourceSchema.Name)
            .SetCode(LogEntryCodes.RequireInvalidFields)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(requireDirective)
            .SetSchema(sourceSchema)
            .SetExtension("errors", errors)
            .SetExtensionsFormatter(ErrorFormatter)
            .Build();
    }

    public static LogEntry RequireInvalidFieldType(
        Directive requireDirective,
        MutableInputFieldDefinition argument,
        MutableSchemaDefinition schema)
    {
        var coordinate = argument.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_RequireInvalidFieldType, coordinate.ToString(), schema.Name)
            .SetCode(LogEntryCodes.RequireInvalidFieldType)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(requireDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry RequireInvalidSyntax(
        Directive requireDirective,
        MutableInputFieldDefinition argument,
        MutableSchemaDefinition schema)
    {
        var coordinate = argument.Coordinate;

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_RequireInvalidSyntax, coordinate.ToString(), schema.Name)
            .SetCode(LogEntryCodes.RequireInvalidSyntax)
            .SetSeverity(LogSeverity.Error)
            .SetCoordinate(coordinate)
            .SetTypeSystemMember(requireDirective)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry RootMutationUsed(MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_RootMutationUsed, schema.Name)
            .SetCode(LogEntryCodes.RootMutationUsed)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(schema)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry RootQueryUsed(MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_RootQueryUsed, schema.Name)
            .SetCode(LogEntryCodes.RootQueryUsed)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(schema)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry RootSubscriptionUsed(MutableSchemaDefinition schema)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_RootSubscriptionUsed, schema.Name)
            .SetCode(LogEntryCodes.RootSubscriptionUsed)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(schema)
            .SetSchema(schema)
            .Build();
    }

    public static LogEntry TypeKindMismatch(
        ITypeDefinition type,
        MutableSchemaDefinition schemaA,
        string typeKindA,
        MutableSchemaDefinition schemaB,
        string typeKindB)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_TypeKindMismatch,
                type.Name,
                schemaA.Name,
                typeKindA,
                schemaB.Name,
                typeKindB)
            .SetCode(LogEntryCodes.TypeKindMismatch)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(type)
            .SetSchema(schemaA)
            .Build();
    }

    private static string ErrorFormatter(ImmutableDictionary<string, object?> extensions)
    {
        var errors = (IEnumerable<string>)extensions["errors"]!;
        return "- " + string.Join($"{Environment.NewLine}- ", errors);
    }
}
