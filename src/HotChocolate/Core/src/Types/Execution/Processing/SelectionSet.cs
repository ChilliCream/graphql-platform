using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// A selection set is primarily composed of field selections.
/// When needed a selection set can preserve fragments so that the execution engine
/// can branch the processing of these fragments.
/// </summary>
public sealed class SelectionSet : ISelectionSet
{
    private readonly Selection[] _selections;
    private readonly FrozenDictionary<string, Selection> _responseNameLookup;
    private readonly SelectionLookup _utf8ResponseNameLookup;
    private Flags _flags;

    public SelectionSet(int id, IObjectTypeDefinition type, Selection[] selections, bool isConditional)
    {
        ArgumentNullException.ThrowIfNull(selections);

        if (selections.Length == 0)
        {
            throw new ArgumentException("Selections cannot be empty.", nameof(selections));
        }

        Id = id;
        Type = type;
        _flags = isConditional ? Flags.Conditional : Flags.None;
        _selections = selections;
        _responseNameLookup = _selections.ToFrozenDictionary(t => t.ResponseName);
        _utf8ResponseNameLookup = SelectionLookup.Create(this);
    }

    /// <summary>
    /// Gets an operation unique selection-set identifier of this selection.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Defines if this list needs post-processing for skip and include.
    /// </summary>
    public bool IsConditional => (_flags & Flags.Conditional) == Flags.Conditional;

    /// <summary>
    /// Gets the type that declares this selection set.
    /// </summary>
    public IObjectTypeDefinition Type { get; }

    /// <summary>
    /// Gets the declaring operation.
    /// </summary>
    public Operation DeclaringOperation { get; private set; } = null!;

    IOperation ISelectionSet.DeclaringOperation => DeclaringOperation;

    /// <summary>
    /// Gets the selections that shall be executed.
    /// </summary>
    public ReadOnlySpan<Selection> Selections => _selections;

    IEnumerable<ISelection> ISelectionSet.GetSelections() => _selections;

    internal void Complete(Operation declaringOperation, bool seal)
    {
        if ((_flags & Flags.Sealed) == Flags.Sealed)
        {
            throw new InvalidOperationException("Selection set is already sealed.");
        }

        DeclaringOperation = declaringOperation;

        foreach (var selection in _selections)
        {
            selection.Complete(this, seal);
        }

        if (seal)
        {
            _flags |= Flags.Sealed;
        }
    }

    /// <summary>
    /// Returns a reference to the 0th element of the underlying selections array.
    /// If the selections array is empty, returns a reference to the location where the 0th element
    /// would have been stored. Such a reference may or may not be null.
    /// It can be used for pinning but must never be de-referenced.
    /// This is only meant for use by the execution engine.
    /// </summary>
    internal ref Selection GetSelectionsReference()
        => ref MemoryMarshal.GetReference(_selections.AsSpan());

    [Flags]
    private enum Flags
    {
        None = 0,
        Conditional = 1,
        Sealed = 2
    }

    public override string ToString()
    {
        // this produces the rough structure of the selection set for debugging purposes.
        var sb = new StringBuilder();

        foreach (var selection in _selections)
        {
            if (selection.Type.IsLeafType())
            {
                if (selection.ResponseName.Equals(selection.Field.Name))
                {
                    sb.AppendLine(selection.ResponseName);
                }
                else
                {
                    sb.AppendLine($"{selection.ResponseName}: {selection.Field.Name}");
                }
            }
            else
            {
                if (selection.ResponseName.Equals(selection.Field.Name))
                {
                    sb.AppendLine($"{selection.ResponseName} {{ ... }}");
                }
                else
                {
                    sb.AppendLine($"{selection.ResponseName}: {selection.Field.Name} {{{{ ... }}}}");
                }
            }
        }

        return sb.ToString();
    }
}
