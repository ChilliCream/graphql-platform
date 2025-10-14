using HotChocolate.Language.Utilities;
using HotChocolate.Types;
using static HotChocolate.Properties.ValidationResources;

namespace HotChocolate.Logging;

internal static class LogEntryHelper
{
    public static LogEntry AdditionalArgumentNotNullable(
        IOutputFieldDefinition field,
        IOutputFieldDefinition implementedField,
        IInputValueDefinition argument)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_AdditionalArgumentNotNullable, field.Coordinate.ToString())
            .SetCode(LogEntryCodes.AdditionalArgumentNotNullable)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetField(field)
            .SetImplementedField(implementedField)
            .SetArgument(argument)
            .SetSpecifiedBy(field.DeclaringType.Kind)
            .Build();
    }

    public static LogEntry ArgumentNotImplemented(
        IOutputFieldDefinition field,
        IOutputFieldDefinition implementedField,
        IInputValueDefinition missingArgument)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_ArgumentNotImplemented,
                field.Coordinate.ToString(),
                missingArgument.Name,
                implementedField.DeclaringType.ToTypeNode().Print())
            .SetCode(LogEntryCodes.ArgumentNotImplemented)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetField(field)
            .SetImplementedField(implementedField)
            .SetExtension("missingArgument", missingArgument)
            .SetSpecifiedBy(field.DeclaringType.Kind)
            .Build();
    }

    public static LogEntry DirectiveMissingLocation(IDirectiveDefinition directive)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_DirectiveMissingLocation, directive.Name)
            .SetCode(LogEntryCodes.DirectiveMissingLocation)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(directive)
            .SetSpecifiedBy(TypeKind.Directive)
            .Build();
    }

    public static LogEntry EmptyEnumType(IEnumTypeDefinition enumType)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_EmptyEnumType, enumType.Name)
            .SetCode(LogEntryCodes.EmptyEnumType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(enumType)
            .SetSpecifiedBy(enumType.Kind)
            .Build();
    }

    public static LogEntry EmptyInputObjectType(IInputObjectTypeDefinition inputObjectType)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_EmptyInputObjectType, inputObjectType.Name)
            .SetCode(LogEntryCodes.EmptyInputObjectType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(inputObjectType)
            .SetSpecifiedBy(inputObjectType.Kind)
            .Build();
    }

    public static LogEntry EmptyInterfaceType(IInterfaceTypeDefinition interfaceType)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_EmptyInterfaceType, interfaceType.Name)
            .SetCode(LogEntryCodes.EmptyInterfaceType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(interfaceType)
            .SetSpecifiedBy(interfaceType.Kind)
            .Build();
    }

    public static LogEntry EmptyObjectType(IObjectTypeDefinition objectType)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_EmptyObjectType, objectType.Name)
            .SetCode(LogEntryCodes.EmptyObjectType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(objectType)
            .SetSpecifiedBy(objectType.Kind)
            .Build();
    }

    public static LogEntry EmptyUnionType(IUnionTypeDefinition unionType)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_EmptyUnionType, unionType.Name)
            .SetCode(LogEntryCodes.EmptyUnionType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(unionType)
            .SetSpecifiedBy(unionType.Kind)
            .Build();
    }

    public static LogEntry FieldNotImplemented(
        IComplexTypeDefinition type,
        IOutputFieldDefinition implementedField)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_FieldNotImplemented,
                implementedField.Coordinate.ToString(),
                type.ToTypeNode().Print())
            .SetCode(LogEntryCodes.FieldNotImplemented)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(type)
            .SetImplementedField(implementedField)
            .SetSpecifiedBy(type.Kind)
            .Build();
    }

    public static LogEntry InputObjectCycle(
        IInputObjectTypeDefinition inputObjectType,
        IEnumerable<string> cyclePath)
    {
        var cyclePathArray = cyclePath.ToArray();
        var message = cyclePathArray.Length == 1
            ? string.Format(
                LogEntryHelper_InputObjectCycle_Direct,
                inputObjectType.Name,
                cyclePathArray[0])
            : string.Format(
                LogEntryHelper_InputObjectCycle_Indirect,
                inputObjectType.Name,
                string.Join(", ", cyclePathArray.Select(i => $"'{i}'")));

        return LogEntryBuilder.New()
            .SetMessage(message)
            .SetCode(LogEntryCodes.InputObjectCycle)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(inputObjectType)
            .SetSpecifiedBy(inputObjectType.Kind)
            .Build();
    }

    public static LogEntry InputObjectDefaultValueCycle(
        IInputValueDefinition inputField,
        IEnumerable<string> cyclePath)
    {
        var cyclePathArray = cyclePath.ToArray();
        var message = cyclePathArray.Length == 0
            ? string.Format(
                LogEntryHelper_InputObjectDefaultValueCycle_Direct,
                inputField.Coordinate.ToString())
            : string.Format(
                LogEntryHelper_InputObjectDefaultValueCycle_Indirect,
                inputField.Coordinate.ToString(),
                string.Join(", ", cyclePathArray.Select(i => $"'{i}'")));

        return LogEntryBuilder.New()
            .SetMessage(message)
            .SetCode(LogEntryCodes.InputObjectDefaultValueCycle)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(inputField)
            .SetSpecifiedBy(GetTypeSystemMemberKind(inputField.DeclaringMember))
            .Build();
    }

    public static LogEntry InvalidArgumentDeprecation(IInputValueDefinition inputField)
    {
        var specifiedByTypeKind = inputField.DeclaringMember switch
        {
            IOutputFieldDefinition f => f.DeclaringType.Kind,
            IDirectiveDefinition => TypeKind.Directive,
            _ => throw new InvalidOperationException()
        };

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_InvalidArgumentDeprecation, inputField.Coordinate.ToString())
            .SetCode(LogEntryCodes.InvalidArgumentDeprecation)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(inputField)
            .SetSpecifiedBy(specifiedByTypeKind)
            .Build();
    }

    public static LogEntry InvalidArgumentType(
        IOutputFieldDefinition field,
        IOutputFieldDefinition implementedField,
        IInputValueDefinition argument,
        IInputValueDefinition implementedArgument)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_InvalidArgumentType,
                argument.Name,
                field.Coordinate.ToString(),
                implementedArgument.Type.ToTypeNode().Print(),
                implementedField.DeclaringType.Name)
            .SetCode(LogEntryCodes.InvalidArgumentType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(argument)
            .SetArgument(argument)
            .SetImplementedArgument(implementedArgument)
            .SetSpecifiedBy(field.DeclaringType.Kind)
            .Build();
    }

    public static LogEntry InvalidFieldDeprecation(
        string implementedTypeName,
        IOutputFieldDefinition implementedField,
        IComplexTypeDefinition type,
        IOutputFieldDefinition field)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_InvalidFieldDeprecation,
                field.Coordinate.ToString(),
                implementedTypeName)
            .SetCode(LogEntryCodes.InvalidFieldDeprecation)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetField(field)
            .SetImplementedField(implementedField)
            .SetSpecifiedBy(type.Kind)
            .Build();
    }

    public static LogEntry InvalidFieldType(
        IComplexTypeDefinition type,
        IOutputFieldDefinition field,
        IOutputFieldDefinition implementedField)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_InvalidFieldType,
                field.Coordinate.ToString(),
                implementedField.Type.ToTypeNode().Print())
            .SetCode(LogEntryCodes.InvalidFieldType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .SetField(field)
            .SetImplementedField(implementedField)
            .SetSpecifiedBy(type.Kind)
            .Build();
    }

    public static LogEntry InvalidInputFieldDeprecation(IInputValueDefinition inputField)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_InvalidInputFieldDeprecation,
                inputField.Coordinate.ToString())
            .SetCode(LogEntryCodes.InvalidInputFieldDeprecation)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(inputField)
            .SetSpecifiedBy(GetTypeSystemMemberKind(inputField.DeclaringMember))
            .Build();
    }

    public static LogEntry InvalidMemberName(ITypeSystemMember member)
    {
        var coordinate = member is ISchemaCoordinateProvider m ? m.Coordinate.ToString() : "?";

        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_InvalidMemberName, coordinate)
            .SetCode(LogEntryCodes.InvalidMemberName)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(member)
            .SetSpecifiedBy(section: "sec-Names.Reserved-Names")
            .Build();
    }

    public static LogEntry InvalidOneOfField(IInputValueDefinition inputField)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_InvalidOneOfField, inputField.Name)
            .SetCode(LogEntryCodes.InvalidOneOfField)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(inputField)
            .SetSpecifiedBy(GetTypeSystemMemberKind(inputField.DeclaringMember))
            .Build();
    }

    public static LogEntry NotTransitivelyImplemented(
        IComplexTypeDefinition complexType,
        IComplexTypeDefinition implementedType)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_NotTransitivelyImplemented, complexType.Name)
            .SetCode(LogEntryCodes.NotTransitivelyImplemented)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(complexType)
            .SetImplementedType(implementedType)
            .SetSpecifiedBy(complexType.Kind)
            .Build();
    }

    public static LogEntry SelfImplementation(IInterfaceTypeDefinition interfaceType)
    {
        return LogEntryBuilder.New()
            .SetMessage(LogEntryHelper_SelfImplementation, interfaceType.Name)
            .SetCode(LogEntryCodes.SelfImplementation)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(interfaceType)
            .SetSpecifiedBy(interfaceType.Kind)
            .Build();
    }

    public static LogEntry UndefinedArgumentType(IInputValueDefinition argument)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_UndefinedArgumentType,
                argument.Type.NamedType().Name,
                argument.Coordinate)
            .SetCode(LogEntryCodes.UndefinedArgumentType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(argument)
            .Build();
    }

    public static LogEntry UndefinedFieldType(IFieldDefinition field)
    {
        return LogEntryBuilder.New()
            .SetMessage(
                LogEntryHelper_UndefinedFieldType,
                field.Type.NamedType().Name,
                field.Coordinate)
            .SetCode(LogEntryCodes.UndefinedFieldType)
            .SetSeverity(LogSeverity.Error)
            .SetTypeSystemMember(field)
            .Build();
    }

    private static TypeKind GetTypeSystemMemberKind(ITypeSystemMember member)
    {
        return member switch
        {
            IType t => t.Kind,
            IDirectiveDefinition => TypeKind.Directive,
            _ => throw new InvalidOperationException()
        };
    }

    extension(LogEntryBuilder builder)
    {
        private LogEntryBuilder SetArgument(IInputValueDefinition argument)
        {
            return builder.SetExtension("argument", argument);
        }

        private LogEntryBuilder SetField(IFieldDefinition field, string name = "field")
        {
            return builder.SetExtension(name, field);
        }

        private LogEntryBuilder SetImplementedArgument(IInputValueDefinition argument)
        {
            return builder.SetField(argument, "implementedArgument");
        }

        private LogEntryBuilder SetImplementedField(IOutputFieldDefinition field)
        {
            return builder.SetField(field, "implementedField");
        }

        private LogEntryBuilder SetImplementedType(ITypeDefinition type)
        {
            return builder.SetExtension("implementedType", type);
        }

        private LogEntryBuilder SetSpecifiedBy(TypeKind typeKind)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            var section = typeKind switch
            {
                TypeKind.Directive => "sec-Type-System.Directives.Type-Validation",
                TypeKind.Enum => "sec-Enums.Type-Validation",
                TypeKind.InputObject => "sec-Input-Objects.Type-Validation",
                TypeKind.Interface => "sec-Interfaces.Type-Validation",
                TypeKind.Object => "sec-Objects.Type-Validation",
                TypeKind.Union => "sec-Unions.Type-Validation",
                _ => throw new InvalidOperationException()
            };

            return builder.SetSpecifiedBy(section);
        }

        private LogEntryBuilder SetSpecifiedBy(string section)
        {
            return builder.SetExtension(
                "specifiedBy",
                $"https://spec.graphql.org/September2025/#{section}");
        }
    }
}
