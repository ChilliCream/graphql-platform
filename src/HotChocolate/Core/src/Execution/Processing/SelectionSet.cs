using System;
using System.Collections.Generic;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// A selection set is primarily composed of field selections.
/// When needed a selection set can preserve fragments so that the execution engine
/// can branch the processing of these fragments.
/// </summary>
internal sealed class SelectionSet : ISelectionSet
{
    private static readonly Fragment[] _empty = Array.Empty<Fragment>();
    private readonly IReadOnlyList<Selection> _selections;
    private readonly IReadOnlyList<Fragment> _fragments;
    private Flags _flags;

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionSet"/>.
    /// </summary>
    /// <param name="selections">
    /// A list of executable field selections.
    /// </param>
    /// <param name="isConditional">
    /// Defines if this list needs post processing for skip and include.
    /// </param>
    public SelectionSet(
        IReadOnlyList<Selection> selections,
        bool isConditional)
    {
        _selections = selections;
        _fragments = _empty;
        _flags = isConditional ? Flags.Conditional : Flags.None;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionSet"/>.
    /// </summary>
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
        IReadOnlyList<Selection> selections,
        IReadOnlyList<Fragment>? fragments,
        bool isConditional)
    {
        _selections = selections;
        _fragments = fragments ?? _empty;
        _flags = isConditional ? Flags.Conditional : Flags.None;
    }

    /// <inheritdoc />
    public bool IsConditional => (_flags & Flags.Conditional) != Flags.Conditional;

    /// <inheritdoc />
    public IReadOnlyList<ISelection> Selections => _selections;

    /// <inheritdoc />
    public IReadOnlyList<IFragment> Fragments => _fragments;

    /// <summary>
    /// Gets an empty selection set.
    /// </summary>
    public static SelectionSet Empty { get; } = new(Array.Empty<Selection>(), false);

    internal void Seal(int selectionSetId)
    {
        if ((_flags & Flags.Sealed) != Flags.Sealed)
        {
            for (var i = 0; i < _selections.Count; i++)
            {
                _selections[i].Seal(selectionSetId);
            }

            _flags |= Flags.Sealed;
        }
    }

    [Flags]
    private enum Flags
    {
        None = 0,
        Conditional = 1,
        Sealed = 2
    }
}
