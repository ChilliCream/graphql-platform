using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Logging;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// Object and interface types must be super-sets of all interfaces that they implement.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Objects.Type-Validation">
/// Specification (Objects)
/// </seealso>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation">
/// Specification (Interfaces)
/// </seealso>
public sealed class ValidImplementationsRule : IValidationEventHandler<ComplexTypeEvent>
{
    /// <summary>
    /// Checks that object and interface types are valid implementations of all interfaces they
    /// implement.
    /// </summary>
    public void Handle(ComplexTypeEvent @event, ValidationContext context)
    {
        var complexType = @event.ComplexType;

        foreach (var implementedType in complexType.Implements)
        {
            if (!IsValidImplementation(complexType, implementedType, out var logEntries))
            {
                context.Log.Write(logEntries);
            }
        }
    }

    // https://spec.graphql.org/September2025/#IsValidImplementation()
    private static bool IsValidImplementation(
        IComplexTypeDefinition type,
        IInterfaceTypeDefinition implementedType,
        out List<LogEntry> logEntries)
    {
        logEntries = [];

        if (!IsFullyImplementingInterface(type, implementedType))
        {
            logEntries.Add(NotTransitivelyImplemented(type, implementedType));
        }

        foreach (var implementedField in implementedType.Fields)
        {
            if (type.Fields.TryGetField(implementedField.Name, out var field))
            {
                if (!ValidateArguments(field, implementedField, out var argumentErrors))
                {
                    logEntries.AddRange(argumentErrors);
                }

                if (!IsValidImplementationFieldType(field.Type, implementedField.Type))
                {
                    logEntries.Add(InvalidFieldType(type, field, implementedField));
                }

                if (field.IsDeprecated && !implementedField.IsDeprecated)
                {
                    logEntries.Add(InvalidFieldDeprecation(
                        implementedType.Name,
                        implementedField,
                        type,
                        field));
                }
            }
            else
            {
                logEntries.Add(FieldNotImplemented(type, implementedField));
            }
        }

        return logEntries.Count == 0;
    }

    private static bool IsFullyImplementingInterface(
        IComplexTypeDefinition type,
        IInterfaceTypeDefinition implementedType)
    {
        foreach (var interfaceType in implementedType.Implements)
        {
            if (!type.IsImplementing(interfaceType))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateArguments(
        IOutputFieldDefinition field,
        IOutputFieldDefinition implementedField,
        out List<LogEntry> logEntries)
    {
        logEntries = [];
        var implArgs = implementedField.Arguments.ToDictionary(t => t.Name);

        foreach (var argument in field.Arguments)
        {
            if (implArgs.Remove(argument.Name, out var implementedArgument))
            {
                if (!argument.Type.IsStructurallyEqual(implementedArgument.Type))
                {
                    logEntries.Add(
                        InvalidArgumentType(
                            field,
                            implementedField,
                            argument,
                            implementedArgument));
                }
            }
            else if (argument.Type.IsNonNullType())
            {
                logEntries.Add(
                    AdditionalArgumentNotNullable(
                        field,
                        implementedField,
                        argument));
            }
        }

        foreach (var missingArgument in implArgs.Values)
        {
            logEntries.Add(
                ArgumentNotImplemented(
                    field,
                    implementedField,
                    missingArgument));
        }

        return logEntries.Count == 0;
    }

    // https://spec.graphql.org/September2025/#IsValidImplementationFieldType()
    private static bool IsValidImplementationFieldType(
        IOutputType fieldType,
        IOutputType implementedType)
    {
        while (true)
        {
            if (fieldType.IsNonNullType())
            {
                if (!implementedType.IsNonNullType())
                {
                    fieldType = (IOutputType)fieldType.InnerType();
                    continue;
                }

                fieldType = (IOutputType)fieldType.InnerType();
                implementedType = (IOutputType)implementedType.InnerType();
                continue;
            }

            if (implementedType.IsNonNullType())
            {
                return false;
            }

            if (fieldType.IsListType() && implementedType.IsListType())
            {
                fieldType = (IOutputType)fieldType.ElementType();
                implementedType = (IOutputType)implementedType.ElementType();
                continue;
            }

            return IsSubType(fieldType, implementedType);
        }
    }

    // https://spec.graphql.org/September2025/#IsSubType()
    private static bool IsSubType(IOutputType fieldType, IOutputType implementedType)
    {
        if (ReferenceEquals(fieldType, implementedType))
        {
            return true;
        }

        switch (fieldType)
        {
            case IObjectTypeDefinition objectType
                when implementedType.Kind is TypeKind.Union
                && implementedType.AsTypeDefinition().IsAssignableFrom(objectType):
            case IComplexTypeDefinition complexType
                when implementedType.Kind is TypeKind.Interface
                && complexType.IsImplementing(implementedType.NamedType().Name):
                return true;
            default:
                return false;
        }
    }
}
