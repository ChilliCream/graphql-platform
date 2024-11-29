using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// A selection set is primarily composed of field selections.
/// When needed a selection set can preserve fragments so that the execution engine
/// can branch the processing of these fragments.
/// </summary>
internal sealed class SelectionSet : ISelectionSet
{
    private static readonly Fragment[] _empty = [];
    private readonly Selection[] _selections;
    private readonly Fragment[] _fragments;
    private Flags _flags;

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionSet"/>.
    /// </summary>
    /// <param name="id">
    /// The selection set unique id.
    /// </param>
    /// <param name="selections">
    /// A list of executable field selections.
    /// </param>
    /// <param name="fragments">
    /// A list of preserved fragments that can be used to branch of
    /// some of the execution.
    /// </param>
    /// <param name="isConditional">
    /// Defines if this list needs post processing for skip and include.
    /// </param>
    public SelectionSet(
        int id,
        Selection[] selections,
        Fragment[]? fragments,
        bool isConditional)
    {
        Id = id;
        _selections = selections;
        _fragments = fragments ?? _empty;
        _flags = isConditional ? Flags.Conditional : Flags.None;
    }

    /// <inheritdoc />
    public int Id { get; }

    /// <inheritdoc />
    public bool IsConditional => (_flags & Flags.Conditional) == Flags.Conditional;

    /// <inheritdoc />
    public IReadOnlyList<ISelection> Selections => _selections;

    /// <inheritdoc />
    public IReadOnlyList<IFragment> Fragments => _fragments;

    /// <inheritdoc />
    public IOperation DeclaringOperation { get; private set; } = default!;

    /// <summary>
    /// Completes the selection set without sealing it.
    /// </summary>
    internal void Complete(IOperation declaringOperation)
    {
        if ((_flags & Flags.Sealed) != Flags.Sealed)
        {
            DeclaringOperation = declaringOperation;

            for (var i = 0; i < _selections.Length; i++)
            {
                _selections[i].Complete(declaringOperation, this);
            }
        }
    }

    internal void Seal(IOperation declaringOperation)
    {
        if ((_flags & Flags.Sealed) != Flags.Sealed)
        {
            DeclaringOperation = declaringOperation;

            for (var i = 0; i < _selections.Length; i++)
            {
                _selections[i].Seal(declaringOperation, this);
            }

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
        Sealed = 2,
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
