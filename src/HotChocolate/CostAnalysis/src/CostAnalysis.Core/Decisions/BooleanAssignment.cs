namespace HotChocolate.CostAnalysis;

/// <summary>
/// An immutable set of Boolean variable assignments.
/// </summary>
internal sealed class BooleanAssignment
{
    /// <summary>
    /// Gets the assignment with no variables fixed.
    /// </summary>
    public static readonly BooleanAssignment Empty = new(null, string.Empty, false);

    private readonly BooleanAssignment? _parent;
    private readonly string _variable;
    private readonly bool _value;

    private BooleanAssignment(BooleanAssignment? parent, string variable, bool value)
    {
        _parent = parent;
        _variable = variable;
        _value = value;
    }

    /// <summary>
    /// Returns a new assignment that extends this one with
    /// <paramref name="variable"/> fixed to <paramref name="value"/>.
    /// </summary>
    public BooleanAssignment With(string variable, bool value) => new(this, variable, value);

    /// <summary>
    /// Tries to get the value assigned to <paramref name="variable"/>.
    /// </summary>
    public bool TryGetValue(string variable, out bool value)
    {
        for (var node = this; node._parent is not null; node = node._parent)
        {
            if (node._variable == variable)
            {
                value = node._value;
                return true;
            }
        }

        value = false;
        return false;
    }
}
