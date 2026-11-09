using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;

#if NET10_0_OR_GREATER
using System.Buffers;
using System.Text;
#endif

namespace HotChocolate.Fusion.Execution;

internal static class VariableCoercionHelper
{
    private const int LinearScanThreshold = 8;
#if NET10_0_OR_GREATER
    private const int NameStackallocThreshold = 128;
#endif

    public static bool TryCoerceVariableValues(
        IFeatureProvider context,
        ISchemaDefinition schema,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        JsonElement variableValues,
        bool ignoreAdditionalInputFields,
        [NotNullWhen(true)] out Dictionary<string, VariableValue>? coercedVariableValues,
        [NotNullWhen(false)] out IError? error)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(variableDefinitions);

        if (variableValues.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined))
        {
            throw new ArgumentException(
                "Variable values must be a JSON Object.",
                nameof(variableValues));
        }

        Utf8MemoryBuilder? memory = null;
        var hasVariables = variableValues.ValueKind is JsonValueKind.Object;
        var lookup = hasVariables ? TryCreateLookup(variableDefinitions, variableValues) : null;
        coercedVariableValues = [];
        error = null;

        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var variableDefinition = variableDefinitions[i];
            var variableName = variableDefinition.Variable.Name.Value;
            var variableType = AssertInputType(schema, variableDefinition);
            JsonElement propertyValue = default;

            var hasValue = lookup is null
                ? hasVariables && variableValues.TryGetProperty(variableName, out propertyValue)
                : lookup.TryGetValue(variableName, out propertyValue)
                    && propertyValue.ValueKind is not JsonValueKind.Undefined;

            if (!hasValue && variableDefinition.DefaultValue is { } defaultValue)
            {
                coercedVariableValues[variableName] = new VariableValue(variableName, variableType, defaultValue);
                continue;
            }

            if (!hasValue)
            {
                if (variableType.IsNonNullType())
                {
                    throw ThrowHelper.NonNullVariableIsNull(variableDefinition);
                }

                // if we do not have any value, we will not create an entry to the
                // coerced variables.
                continue;
            }

            if (TryCoerceVariableValue(
                context,
                variableDefinition,
                variableType,
                propertyValue,
                ignoreAdditionalInputFields,
                ref memory,
                out var variableValue,
                out error))
            {
                coercedVariableValues[variableName] = variableValue.Value;
            }
            else
            {
                coercedVariableValues = null;
                return false;
            }
        }

        memory?.Seal();
        return true;
    }

    private static Dictionary<string, JsonElement>? TryCreateLookup(
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        JsonElement variableValues)
    {
        if (variableDefinitions.Count <= LinearScanThreshold)
        {
            return null;
        }

        var lookup = new Dictionary<string, JsonElement>(variableDefinitions.Count);
        var maxNameLength = 0;

        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var name = variableDefinitions[i].Variable.Name.Value;
            lookup[name] = default;
            maxNameLength = Math.Max(maxNameLength, name.Length);
        }

#if NET10_0_OR_GREATER
        var spanLookup = lookup.GetAlternateLookup<ReadOnlySpan<char>>();
        var nameBuffer = maxNameLength <= NameStackallocThreshold
            ? stackalloc char[NameStackallocThreshold]
            : new char[maxNameLength];
#endif

        // like with TryGetProperty the last property with a given name wins.
        foreach (var property in variableValues.EnumerateObject())
        {
#if NET10_0_OR_GREATER
            var utf8Name = JsonMarshal.GetRawUtf8PropertyName(property);

            if (utf8Name.IndexOf((byte)'\\') == -1)
            {
                // variable names are ASCII, so a name that is not ASCII or is longer
                // than every declared name cannot match.
                if (Ascii.ToUtf16(utf8Name, nameBuffer, out var written) is OperationStatus.Done
                    && spanLookup.TryGetValue(nameBuffer[..written], out var declaredName, out _))
                {
                    lookup[declaredName] = property.Value;
                }

                continue;
            }
#endif

            ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(lookup, property.Name);

            if (!Unsafe.IsNullRef(ref value))
            {
                value = property.Value;
            }
        }

        return lookup;
    }

    private static bool TryCoerceVariableValue(
        IFeatureProvider context,
        VariableDefinitionNode variableDefinition,
        IInputType variableType,
        JsonElement value,
        bool ignoreAdditionalInputFields,
        ref Utf8MemoryBuilder? memory,
        [NotNullWhen(true)] out VariableValue? variableValue,
        [NotNullWhen(false)] out IError? error)
    {
        var coercion = new JsonVariableCoercion(context, ref memory, ignoreAdditionalInputFields);
        return coercion.TryCoerceVariableValue(
            variableDefinition.Variable.Name.Value,
            variableType,
            value,
            out variableValue,
            out error);
    }

    private static IInputType AssertInputType(
        ISchemaDefinition schema,
        VariableDefinitionNode variableDefinition)
    {
        if (schema.Types.TryGetType(variableDefinition.Type, out IInputType? type))
        {
            return type;
        }

        throw ThrowHelper.VariableIsNotAnInputType(variableDefinition);
    }
}
