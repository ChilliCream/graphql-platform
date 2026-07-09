using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Logging;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// Input object default values must not form cycles.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation">
/// Specification
/// </seealso>
public sealed class NoInputObjectDefaultValueCycleRule : IValidationEventHandler<InputObjectTypesEvent>
{
    /// <summary>
    /// Checks that input object default values do not form cycles.
    /// </summary>
    public void Handle(InputObjectTypesEvent @event, ValidationContext context)
    {
        var inputObjectTypes = @event.InputObjectTypes;
        var cycleValidationContext = new CycleValidationContext();

        foreach (var inputObjectType in inputObjectTypes)
        {
            // Start with an empty object as a way to visit every field in this input
            // object type and apply every default value.
            InputObjectDefaultValueHasCycle(
                inputObjectType,
                new ObjectValueNode(),
                cycleValidationContext);
        }

        context.Log.Write(cycleValidationContext.LogEntries);
    }

    private static void InputObjectDefaultValueHasCycle(
        IInputObjectTypeDefinition inputObject,
        IValueNode defaultValue,
        CycleValidationContext context)
    {
        // If the value is a List, recursively check each entry for a cycle.
        // Otherwise, only object values can contain a cycle.
        if (defaultValue is ListValueNode listDefaultValue)
        {
            foreach (var itemValue in listDefaultValue.Items)
            {
                InputObjectDefaultValueHasCycle(inputObject, itemValue, context);
            }

            return;
        }

        if (defaultValue is not ObjectValueNode objectDefaultValue)
        {
            return;
        }

        // Check each defined field for a cycle.
        foreach (var field in inputObject.Fields)
        {
            // Only input object type fields can result in a cycle.
            if (field.Type.NamedType() is not IInputObjectTypeDefinition innerInputObject)
            {
                continue;
            }

            var fieldDefaultValue =
                objectDefaultValue.Fields.FirstOrDefault(f => f.Name.Value == field.Name)?.Value;

            if (fieldDefaultValue is not null)
            {
                // If the provided value has this field defined, recursively check it
                // for cycles.
                InputObjectDefaultValueHasCycle(innerInputObject, fieldDefaultValue, context);
            }
            else
            {
                // Otherwise check this field's default value for cycles.
                InputFieldDefaultValueHasCycle(field, innerInputObject, context);
            }
        }
    }

    private static void InputFieldDefaultValueHasCycle(
        IInputValueDefinition field,
        IInputObjectTypeDefinition fieldType,
        CycleValidationContext context)
    {
        // Only a field with a default value can result in a cycle.
        var defaultValue = field.DefaultValue;
        if (defaultValue is null)
        {
            return;
        }

        // Check to see if there is cycle.
        if (context.FieldPathIndex.TryGetValue(field, out var cycleIndex))
        {
            var cyclePath = context.FieldPath.Skip(cycleIndex);
            context.LogEntries.Add(InputObjectDefaultValueCycle(field, cyclePath));
            return;
        }

        // Recurse into this field's default value once, tracking the path.
        if (context.VisitedFields.Add(field))
        {
            context.FieldPath.Add(field.Coordinate.ToString());
            context.FieldPathIndex.Add(field, context.FieldPath.Count);

            InputObjectDefaultValueHasCycle(fieldType, defaultValue, context);

            context.FieldPath.Pop();
            context.FieldPathIndex.Remove(field);
        }
    }

    private sealed class CycleValidationContext
    {
        public HashSet<IInputValueDefinition> VisitedFields { get; } = [];

        public Dictionary<IInputValueDefinition, int> FieldPathIndex { get; } = [];

        public List<string> FieldPath { get; } = [];

        public List<LogEntry> LogEntries { get; } = [];
    }
}
