using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Logging;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// If an Input Object references itself either directly or through referenced Input Objects, at
/// least one of the fields in the chain of references must be either a nullable or a List type.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation">
/// Specification
/// </seealso>
public sealed class NoInputObjectCycleRule : IValidationEventHandler<InputObjectTypesEvent>
{
    /// <summary>
    /// Checks that there are no cycles in input object type definitions.
    /// </summary>
    public void Handle(InputObjectTypesEvent @event, ValidationContext context)
    {
        var inputObjectTypes = @event.InputObjectTypes;
        var cycleValidationContext = new CycleValidationContext();

        foreach (var inputObjectType in inputObjectTypes)
        {
            InputObjectHasCycle(inputObjectType, cycleValidationContext);
        }

        context.Log.Write(cycleValidationContext.LogEntries);
    }

    private static void InputObjectHasCycle(
        IInputObjectTypeDefinition inputObjectType,
        CycleValidationContext context)
    {
        if (!context.VisitedTypes.Add(inputObjectType))
        {
            return;
        }

        context.FieldPathIndexByType[inputObjectType] = context.FieldPath.Count;

        foreach (var field in inputObjectType.Fields)
        {
            var unwrappedType = UnwrapCompletelyIfRequired(field.Type);
            if (unwrappedType is not IInputObjectTypeDefinition innerInputObjectType)
            {
                continue;
            }

            context.FieldPath.Push(field.Coordinate.ToString());

            if (context.FieldPathIndexByType.TryGetValue(innerInputObjectType, out var cycleIndex))
            {
                var cyclePath = context.FieldPath.Skip(cycleIndex);
                context.LogEntries.Add(InputObjectCycle(innerInputObjectType, cyclePath));
            }
            else
            {
                InputObjectHasCycle(innerInputObjectType, context);
            }

            context.FieldPath.Pop();
        }

        context.FieldPathIndexByType.Remove(inputObjectType);
    }

    private static IType? UnwrapCompletelyIfRequired(IType type)
    {
        while (true)
        {
            if (type.Kind == TypeKind.NonNull)
            {
                type = ((NonNullType)type).NullableType;
            }
            else
            {
                return null;
            }

            return type.Kind switch
            {
                TypeKind.List => null,
                _ => type
            };
        }
    }

    private sealed class CycleValidationContext
    {
        public HashSet<IInputObjectTypeDefinition> VisitedTypes { get; } = [];

        public Dictionary<IInputObjectTypeDefinition, int> FieldPathIndexByType { get; } = [];

        public List<string> FieldPath { get; } = [];

        public List<LogEntry> LogEntries { get; } = [];
    }
}
