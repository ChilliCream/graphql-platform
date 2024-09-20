using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// This struct represents the include condition of a Field, InlineFragment or FragmentSpread.
/// </summary>
public readonly struct IncludeCondition : IEquatable<IncludeCondition>
{
    internal IncludeCondition(IValueNode skip, IValueNode include)
    {
        Skip = skip;
        Include = include;
    }

    /// <summary>
    /// Gets the skip value.
    /// </summary>
    public IValueNode Skip { get; }

    /// <summary>
    /// Gets the include value.
    /// </summary>
    public IValueNode Include { get; }

    /// <summary>
    /// If <see cref="Skip"/> and <see cref="Include"/> are null then
    /// there is no valid include condition.
    /// </summary>
    public bool IsDefault => Skip is null && Include is null;

    /// <summary>
    /// Specifies if selections with this include condition are included with the
    /// current variable values.
    /// </summary>
    /// <param name="variables">
    /// The variable values.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if selections with this include condition are included.
    /// </returns>
    public bool IsIncluded(IVariableValueCollection variables)
    {
        if (variables is null)
        {
            throw new ArgumentNullException(nameof(variables));
        }

        if (Skip is null || Include is null)
        {
            return true;
        }

        var skip = false;

        if (Skip.Kind is SyntaxKind.BooleanValue)
        {
            skip = ((BooleanValueNode)Skip).Value;
        }
        else if (Skip.Kind is SyntaxKind.Variable)
        {
            skip = variables.GetVariable<bool>(((VariableNode)Skip).Name.Value);
        }

        var include = true;

        if (Include.Kind is SyntaxKind.BooleanValue)
        {
            include = ((BooleanValueNode)Include).Value;
        }
        else if (Include.Kind is SyntaxKind.Variable)
        {
            include = variables.GetVariable<bool>(((VariableNode)Include).Name.Value);
        }

        return !skip && include;
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true" /> if the current object is equal to the
    /// <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(IncludeCondition other)
        => Skip.Equals(other.Skip, SyntaxComparison.Syntax) &&
            Include.Equals(other.Include, SyntaxComparison.Syntax);

    /// <summary>
    /// Indicates whether this instance and a specified object are equal.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current instance.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> and this instance are the same
    /// type and represent the same value; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is IncludeCondition other && Equals(other);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer that is the hash code for this instance.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(
            SyntaxComparer.BySyntax.GetHashCode(Skip),
            SyntaxComparer.BySyntax.GetHashCode(Include));

    /// <summary>
    /// Tries to extract the include condition from a field.
    /// </summary>
    /// <param name="selection">
    /// The selection to extract the include condition from.
    /// </param>
    /// <returns>
    /// Returns true if the selection has a custom visibility configuration.
    /// </returns>
    public static IncludeCondition FromSelection(ISelectionNode selection)
    {
        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        IValueNode? skip = null;
        IValueNode? include = null;

        if (selection.Directives.Count == 0)
        {
            return default;
        }

        for (var i = 0; i < selection.Directives.Count; i++)
        {
            var directive = selection.Directives[i];

            if (directive.Arguments.Count != 1)
            {
                // the skip and include arguments have a single argument.
                continue;
            }

            if (directive.Name.Value.EqualsOrdinal(WellKnownDirectives.Skip))
            {
                skip = directive.Arguments[0].Value;
            }

            if (directive.Name.Value.EqualsOrdinal(WellKnownDirectives.Include))
            {
                include = directive.Arguments[0].Value;
            }

            if (skip is not null && include is not null)
            {
                break;
            }
        }

        if (skip is null && include is null)
        {
            return default;
        }

        return new IncludeCondition(
            skip ?? NullValueNode.Default,
            include ?? NullValueNode.Default);
    }
}
