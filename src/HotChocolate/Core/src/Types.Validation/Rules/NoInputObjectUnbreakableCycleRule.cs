using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Logging;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// An Input Object must not reference itself through a cycle that cannot be broken. A field
/// breaks a cycle when its type is a list, a nullable type on a non-OneOf Input Object, a type
/// other than an Input Object, or an Input Object that can itself be provided a finite value.
/// </summary>
/// <seealso href="https://github.com/graphql/graphql-spec/pull/1211">
/// Specification
/// </seealso>
public sealed class NoInputObjectUnbreakableCycleRule
    : IValidationEventHandler<InputObjectTypesEvent>
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
                context.LogEntries.Add(
                    InputObjectUnbreakableCycle(innerInputObjectType, cyclePath));
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
