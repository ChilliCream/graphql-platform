using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// Checks that all field and argument types are defined in the schema.
/// </summary>
public sealed class TypeIsDefinedRule
    : IValidationEventHandler<FieldEvent>
    , IValidationEventHandler<ArgumentEvent>
{
    /// <summary>
    /// Checks that all field types are defined in the schema.
    /// </summary>
    public void Handle(FieldEvent @event, ValidationContext context)
    {
        var field = @event.Field;
        var fieldType = field.Type.AsTypeDefinition();

        if (fieldType.IsIntrospectionType)
        {
            return;
        }

        var isSpecType =
            fieldType is IScalarTypeDefinition scalarType
            && SpecScalarNames.IsSpecScalar(scalarType.Name);

        if (fieldType is MissingType
            || (!isSpecType && !context.Schema.Types.ContainsName(fieldType.Name)))
        {
            context.Log.Write(UndefinedFieldType(field));
        }
    }

    /// <summary>
    /// Checks that all argument types are defined in the schema.
    /// </summary>
    public void Handle(ArgumentEvent @event, ValidationContext context)
    {
        var argument = @event.Argument;
        var argumentType = argument.Type.AsTypeDefinition();

        if (argumentType.IsIntrospectionType)
        {
            return;
        }

        var isSpecType =
            argumentType is IScalarTypeDefinition scalarType
            && SpecScalarNames.IsSpecScalar(scalarType.Name);

        if (argumentType is MissingType
            || (!isSpecType && !context.Schema.Types.ContainsName(argumentType.Name)))
        {
            context.Log.Write(UndefinedArgumentType(argument));
        }
    }
}
