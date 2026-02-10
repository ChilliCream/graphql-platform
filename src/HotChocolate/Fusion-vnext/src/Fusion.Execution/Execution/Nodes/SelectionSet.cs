using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

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
    private bool _isSealed;

    public SelectionSet(int id, IObjectTypeDefinition type, Selection[] selections, bool isConditional)
    {
        ArgumentNullException.ThrowIfNull(selections);

        if (selections.Length == 0)
        {
            throw new ArgumentException("Selections cannot be empty.", nameof(selections));
        }

        Id = id;
        Type = type;
        IsConditional = isConditional;
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
    public bool IsConditional { get; }

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

    public bool HasIncrementalParts => throw new NotImplementedException();

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

    internal void Seal(Operation operation)
    {
        if (_isSealed)
        {
            throw new InvalidOperationException("Selection set is already sealed.");
        }

        _isSealed = true;
        DeclaringOperation = operation;

        foreach (var selection in Selections)
        {
            selection.Seal(this);
        }
    }
}
