using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

#if NET10_0_OR_GREATER
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The variable definitions of an operation with their input types and error paths.
/// </summary>
internal sealed class VariableCoercionPlan
{
    private const int LinearScanThreshold = 8;
#if NET10_0_OR_GREATER
    private const int NameStackallocThreshold = 128;
#endif

    private readonly IReadOnlyList<VariableDefinitionNode> _definitions;
    private readonly IInputType?[] _types;
    private readonly Path?[] _paths;
    private readonly Dictionary<string, int>? _slotByName;
    private readonly int[]? _slots;
#if NET10_0_OR_GREATER
    private readonly int _maxNameLength;
#endif

    /// <summary>
    /// Initializes a new instance of <see cref="VariableCoercionPlan"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema that resolves the variable types.
    /// </param>
    /// <param name="variableDefinitions">
    /// The variable definitions of the operation.
    /// </param>
    public VariableCoercionPlan(
        ISchemaDefinition schema,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(variableDefinitions);

        var count = variableDefinitions.Count;
        _definitions = variableDefinitions;
        _types = new IInputType?[count];
        _paths = new Path?[count];

        for (var i = 0; i < count; i++)
        {
            _types[i] = schema.Types.TryGetType(variableDefinitions[i].Type, out IInputType? type)
                ? type
                : null;
        }

        if (count <= LinearScanThreshold)
        {
            return;
        }

        _slotByName = new Dictionary<string, int>(count);
        _slots = new int[count];

        for (var i = 0; i < count; i++)
        {
            var name = variableDefinitions[i].Variable.Name.Value;

            if (!_slotByName.TryGetValue(name, out var slot))
            {
                slot = i;
                _slotByName.Add(name, slot);
            }

            _slots[i] = slot;
#if NET10_0_OR_GREATER
            _maxNameLength = Math.Max(_maxNameLength, name.Length);
#endif
        }
    }

    /// <summary>
    /// Gets the number of variable definitions.
    /// </summary>
    public int Count => _definitions.Count;

    /// <summary>
    /// Gets a value indicating whether the plan assigns a slot to every variable.
    /// <see cref="GetSlot"/> and <see cref="ReadValues"/> are valid only when this is <c>true</c>.
    /// </summary>
    public bool HasSlots => _slots is not null;

    /// <summary>
    /// Gets the variable definition at <paramref name="index"/>.
    /// </summary>
    public VariableDefinitionNode GetDefinition(int index) => _definitions[index];

    /// <summary>
    /// Gets the input type of the variable at <paramref name="index"/>,
    /// or <c>null</c> if its type is not an input type of the schema.
    /// </summary>
    public IInputType? GetInputType(int index) => _types[index];

    /// <summary>
    /// Gets the path that errors of the variable at <paramref name="index"/> are reported with.
    /// </summary>
    public Path GetPath(int index)
        => _paths[index] ??= Path.Root.Append(_definitions[index].Variable.Name.Value);

    /// <summary>
    /// Gets the slot that holds the provided value of the variable at <paramref name="index"/>.
    /// Valid only when <see cref="HasSlots"/> is <c>true</c>.
    /// </summary>
    public int GetSlot(int index) => _slots![index];

    /// <summary>
    /// Writes the value that <paramref name="variableValues"/> provides for a declared variable
    /// into the slot of that variable. Slots of variables without a value are left untouched.
    /// Valid only when <see cref="HasSlots"/> is <c>true</c>.
    /// </summary>
    /// <param name="variableValues">
    /// The JSON object with the provided variable values.
    /// </param>
    /// <param name="values">
    /// The slots, at least <see cref="Count"/> elements.
    /// </param>
    public void ReadValues(JsonElement variableValues, Span<JsonElement> values)
    {
        var slotByName = _slotByName!;

#if NET10_0_OR_GREATER
        var spanLookup = slotByName.GetAlternateLookup<ReadOnlySpan<char>>();
        var nameBuffer = _maxNameLength <= NameStackallocThreshold
            ? stackalloc char[NameStackallocThreshold]
            : new char[_maxNameLength];
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
                    && spanLookup.TryGetValue(nameBuffer[..written], out var declaredSlot))
                {
                    values[declaredSlot] = property.Value;
                }

                continue;
            }
#endif

            if (slotByName.TryGetValue(property.Name, out var slot))
            {
                values[slot] = property.Value;
            }
        }
    }
}
