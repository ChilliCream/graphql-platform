using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
    private const int MaxLinearLookupSelections = 7;

    private readonly Selection[] _selections;
    private readonly FrozenDictionary<string, Selection> _responseNameLookup;
    private readonly SelectionLookup _utf8ResponseNameLookup;
    private ObjectTemplate _objectTemplate;
    private bool _isSealed;

    public SelectionSet(
        int id,
        IComplexTypeDefinition type,
        Selection[] selections,
        bool isConditional,
        bool hasIncrementalParts,
        Selection? declaringSelection)
    {
        ArgumentNullException.ThrowIfNull(selections);

        if (selections.Length == 0)
        {
            throw new ArgumentException("Selections cannot be empty.", nameof(selections));
        }

        Id = id;
        Type = type;
        IsConditional = isConditional;
        HasIncrementalParts = hasIncrementalParts;
        DeclaringSelection = declaringSelection;
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
    /// <remarks>
    /// This is an object type for a fully resolved (concrete) selection set. It can be an
    /// interface type while an element produced by an <c>@interfaceObject</c> stand-in still
    /// awaits identity recovery through its covering lookup.
    /// </remarks>
    public IComplexTypeDefinition Type { get; }

    /// <summary>
    /// Gets the field selection whose child selection set this is, or <c>null</c> for the
    /// operation's root selection set. Used to recompute the concrete selection set when an
    /// opaque interface-typed element recovers its identity through a covering lookup.
    /// </summary>
    public Selection? DeclaringSelection { get; }

    /// <summary>
    /// Gets the declaring operation.
    /// </summary>
    public Operation DeclaringOperation { get; private set; } = null!;

    IOperation ISelectionSet.DeclaringOperation => DeclaringOperation;

    /// <summary>
    /// Gets the selections that shall be executed.
    /// </summary>
    public ReadOnlySpan<Selection> Selections => _selections;

    /// <summary>
    /// Gets a value indicating whether the selection set contains deferred selections.
    /// </summary>
    public bool HasIncrementalParts { get; }

    /// <summary>
    /// Gets the prepared result row block shared by every object instance of this selection
    /// set. It is built when the selection set is sealed and reused across all requests of
    /// the declaring operation.
    /// </summary>
    internal ObjectTemplate ObjectTemplate => _objectTemplate;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSelection(ReadOnlySpan<byte> utf8ResponseName, [NotNullWhen(true)] out Selection? selection)
    {
        // Many execution selection sets are small, so direct span comparisons avoid
        // hash computation/probing in the hot path.
        var selections = _selections;

        if (selections.Length <= MaxLinearLookupSelections)
        {
            for (var i = 0; i < selections.Length; i++)
            {
                var candidate = selections[i];

                if (utf8ResponseName.SequenceEqual(candidate.Utf8ResponseName))
                {
                    selection = candidate;
                    return true;
                }
            }

            selection = default;
            return false;
        }

        return _utf8ResponseNameLookup.TryGetSelection(utf8ResponseName, out selection);
    }

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

        _objectTemplate = ObjectTemplate.Create(this);
    }
}
