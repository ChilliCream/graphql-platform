using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
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
    private Operation? _declaringOperation;

    internal SelectionSet(
        int id,
        SelectionPath path,
        IObjectTypeDefinition type,
        Selection[] selections,
        bool isConditional,
        bool hasDeferredSelections = false)
    {
        ArgumentNullException.ThrowIfNull(selections);

        Id = id;
        Path = path;
        Type = type;
        _flags = isConditional ? Flags.Conditional : Flags.None;

        if (hasDeferredSelections)
        {
            _flags |= Flags.HasDeferredSelections;
        }

        _selections = selections;
        _responseNameLookup = _selections.ToFrozenDictionary(t => t.ResponseName);
        _utf8ResponseNameLookup = SelectionLookup.Create(this);
    }

    /// <summary>
    /// Gets an operation unique selection-set identifier of this selection.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the path where this selection set is located within the GraphQL operation document.
    /// </summary>
    public SelectionPath Path { get; }

    /// <summary>
    /// Defines if this list needs post-processing for skip and include.
    /// </summary>
    public bool IsConditional => (_flags & Flags.Conditional) == Flags.Conditional;

    /// <inheritdoc />
    public bool HasIncrementalParts => (_flags & Flags.HasDeferredSelections) == Flags.HasDeferredSelections;

    /// <summary>
    /// Gets the type context of this selection set.
    /// </summary>
    public IObjectTypeDefinition Type { get; }

    /// <summary>
    /// Gets the declaring operation.
    /// </summary>
    public Operation DeclaringOperation => _declaringOperation ?? throw ThrowHelper.SelectionSet_NotFullyInitialized();

    IOperation ISelectionSet.DeclaringOperation => DeclaringOperation;

    /// <summary>
    /// Gets the selections that shall be executed.
    /// </summary>
    public ReadOnlySpan<Selection> Selections => _selections;

    IEnumerable<ISelection> ISelectionSet.GetSelections() => _selections;

    /// <summary>
    /// Tries to resolve a selection by name.
    /// </summary>
    /// <param name="responseName">
    /// The selection response name.
    /// </param>
    /// <param name="selection">
    /// The resolved selection.
    /// </param>
    /// <returns>
    /// Returns true if the selection was successfully resolved.
    /// </returns>
    public bool TryGetSelection(string responseName, [NotNullWhen(true)] out Selection? selection)
        => _responseNameLookup.TryGetValue(responseName, out selection);

    /// <summary>
    /// Tries to resolve a selection by name.
    /// </summary>
    /// <param name="utf8ResponseName">
    /// The selection response name.
    /// </param>
    /// <param name="selection">
    /// The resolved selection.
    /// </param>
    /// <returns>
    /// Returns true if the selection was successfully resolved.
    /// </returns>
    public bool TryGetSelection(ReadOnlySpan<byte> utf8ResponseName, [NotNullWhen(true)] out Selection? selection)
        => _utf8ResponseNameLookup.TryGetSelection(utf8ResponseName, out selection);

    internal void Complete(Operation declaringOperation)
    {
        if ((_flags & Flags.Sealed) == Flags.Sealed)
        {
            throw new InvalidOperationException("Selection set is already sealed.");
        }

        _declaringOperation = declaringOperation;

        foreach (var selection in _selections)
        {
            selection.Complete(this);
        }

        _flags |= Flags.Sealed;
    }

    [Flags]
    private enum Flags
    {
        None = 0,
        Conditional = 1,
        Sealed = 2,
        HasDeferredSelections = 4
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
