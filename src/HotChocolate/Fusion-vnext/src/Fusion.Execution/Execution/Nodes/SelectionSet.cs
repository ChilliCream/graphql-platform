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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSelection(ReadOnlySpan<byte> utf8ResponseName, [NotNullWhen(true)] out Selection? selection)
    {
        // Most execution selection sets are tiny (1-3 fields), so direct
        // span comparisons avoid hash computation/probing in the hot path.
        var selections = _selections;

        switch (selections.Length)
        {
            case 1:
                var candidate = selections[0];

                if (utf8ResponseName.SequenceEqual(candidate.Utf8ResponseName))
                {
                    selection = candidate;
                    return true;
                }

                selection = default;
                return false;

            case 2:
                var candidate0 = selections[0];

                if (utf8ResponseName.SequenceEqual(candidate0.Utf8ResponseName))
                {
                    selection = candidate0;
                    return true;
                }

                var candidate1 = selections[1];

                if (utf8ResponseName.SequenceEqual(candidate1.Utf8ResponseName))
                {
                    selection = candidate1;
                    return true;
                }

                selection = default;
                return false;

            case 3:
                var firstCandidate = selections[0];

                if (utf8ResponseName.SequenceEqual(firstCandidate.Utf8ResponseName))
                {
                    selection = firstCandidate;
                    return true;
                }

                var secondCandidate = selections[1];

                if (utf8ResponseName.SequenceEqual(secondCandidate.Utf8ResponseName))
                {
                    selection = secondCandidate;
                    return true;
                }

                var thirdCandidate = selections[2];

                if (utf8ResponseName.SequenceEqual(thirdCandidate.Utf8ResponseName))
                {
                    selection = thirdCandidate;
                    return true;
                }

                selection = default;
                return false;

            case 4:
                var firstCandidate4 = selections[0];

                if (utf8ResponseName.SequenceEqual(firstCandidate4.Utf8ResponseName))
                {
                    selection = firstCandidate4;
                    return true;
                }

                var secondCandidate4 = selections[1];

                if (utf8ResponseName.SequenceEqual(secondCandidate4.Utf8ResponseName))
                {
                    selection = secondCandidate4;
                    return true;
                }

                var thirdCandidate4 = selections[2];

                if (utf8ResponseName.SequenceEqual(thirdCandidate4.Utf8ResponseName))
                {
                    selection = thirdCandidate4;
                    return true;
                }

                var fourthCandidate4 = selections[3];

                if (utf8ResponseName.SequenceEqual(fourthCandidate4.Utf8ResponseName))
                {
                    selection = fourthCandidate4;
                    return true;
                }

                selection = default;
                return false;

            case 5:
                var candidate5_0 = selections[0];

                if (utf8ResponseName.SequenceEqual(candidate5_0.Utf8ResponseName))
                {
                    selection = candidate5_0;
                    return true;
                }

                var candidate5_1 = selections[1];

                if (utf8ResponseName.SequenceEqual(candidate5_1.Utf8ResponseName))
                {
                    selection = candidate5_1;
                    return true;
                }

                var candidate5_2 = selections[2];

                if (utf8ResponseName.SequenceEqual(candidate5_2.Utf8ResponseName))
                {
                    selection = candidate5_2;
                    return true;
                }

                var candidate5_3 = selections[3];

                if (utf8ResponseName.SequenceEqual(candidate5_3.Utf8ResponseName))
                {
                    selection = candidate5_3;
                    return true;
                }

                var candidate5_4 = selections[4];

                if (utf8ResponseName.SequenceEqual(candidate5_4.Utf8ResponseName))
                {
                    selection = candidate5_4;
                    return true;
                }

                selection = default;
                return false;

            case 6:
                var candidate6_0 = selections[0];

                if (utf8ResponseName.SequenceEqual(candidate6_0.Utf8ResponseName))
                {
                    selection = candidate6_0;
                    return true;
                }

                var candidate6_1 = selections[1];

                if (utf8ResponseName.SequenceEqual(candidate6_1.Utf8ResponseName))
                {
                    selection = candidate6_1;
                    return true;
                }

                var candidate6_2 = selections[2];

                if (utf8ResponseName.SequenceEqual(candidate6_2.Utf8ResponseName))
                {
                    selection = candidate6_2;
                    return true;
                }

                var candidate6_3 = selections[3];

                if (utf8ResponseName.SequenceEqual(candidate6_3.Utf8ResponseName))
                {
                    selection = candidate6_3;
                    return true;
                }

                var candidate6_4 = selections[4];

                if (utf8ResponseName.SequenceEqual(candidate6_4.Utf8ResponseName))
                {
                    selection = candidate6_4;
                    return true;
                }

                var candidate6_5 = selections[5];

                if (utf8ResponseName.SequenceEqual(candidate6_5.Utf8ResponseName))
                {
                    selection = candidate6_5;
                    return true;
                }

                selection = default;
                return false;

            case 7:
                var candidate7_0 = selections[0];

                if (utf8ResponseName.SequenceEqual(candidate7_0.Utf8ResponseName))
                {
                    selection = candidate7_0;
                    return true;
                }

                var candidate7_1 = selections[1];

                if (utf8ResponseName.SequenceEqual(candidate7_1.Utf8ResponseName))
                {
                    selection = candidate7_1;
                    return true;
                }

                var candidate7_2 = selections[2];

                if (utf8ResponseName.SequenceEqual(candidate7_2.Utf8ResponseName))
                {
                    selection = candidate7_2;
                    return true;
                }

                var candidate7_3 = selections[3];

                if (utf8ResponseName.SequenceEqual(candidate7_3.Utf8ResponseName))
                {
                    selection = candidate7_3;
                    return true;
                }

                var candidate7_4 = selections[4];

                if (utf8ResponseName.SequenceEqual(candidate7_4.Utf8ResponseName))
                {
                    selection = candidate7_4;
                    return true;
                }

                var candidate7_5 = selections[5];

                if (utf8ResponseName.SequenceEqual(candidate7_5.Utf8ResponseName))
                {
                    selection = candidate7_5;
                    return true;
                }

                var candidate7_6 = selections[6];

                if (utf8ResponseName.SequenceEqual(candidate7_6.Utf8ResponseName))
                {
                    selection = candidate7_6;
                    return true;
                }

                selection = default;
                return false;

        }

        if (selections.Length <= 7)
        {
            return TryGetSelectionLinear(utf8ResponseName, selections, out selection);
        }

        return _utf8ResponseNameLookup.TryGetSelection(utf8ResponseName, out selection);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetSelectionLinear(
        ReadOnlySpan<byte> utf8ResponseName,
        Selection[] selections,
        [NotNullWhen(true)] out Selection? selection)
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
