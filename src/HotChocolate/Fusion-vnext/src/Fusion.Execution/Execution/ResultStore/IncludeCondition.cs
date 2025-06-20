using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal readonly struct IncludeCondition(string? skip, string? include)
    : IEquatable<IncludeCondition>
{
    public string? Skip { get; } = skip;

    public string? Include { get; } = include;

    public bool Equals(IncludeCondition other)
        => string.Equals(Skip, other.Skip, StringComparison.Ordinal)
            && string.Equals(Include, other.Include, StringComparison.Ordinal);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is IncludeCondition other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Skip, Include);

    public static bool TryCreate(FieldNode field, out IncludeCondition includeCondition)
    {
        string? skip = null;
        string? include = null;

        for (var i = 0; i < field.Directives.Count; i++)
        {
            var directive = field.Directives[i];
            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal)
                && directive.Arguments.Count == 1
                && directive.Arguments[0].Value is VariableNode skipVariable)
            {
                skip = skipVariable.Name.Value;
            }
            else if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal)
                && directive.Arguments.Count == 1
                && directive.Arguments[0].Value is VariableNode includeVariable)
            {
                include = includeVariable.Name.Value;
            }

            if (skip is not null && include is not null)
            {
                includeCondition = new IncludeCondition(skip, include);
                return true;
            }
        }

        includeCondition = default;
        return false;
    }

    public static bool TryCreate(InlineFragmentNode field, out IncludeCondition includeCondition)
    {
        string? skip = null;
        string? include = null;

        for (var i = 0; i < field.Directives.Count; i++)
        {
            var directive = field.Directives[i];
            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal)
                && directive.Arguments.Count == 1
                && directive.Arguments[0].Value is VariableNode skipVariable)
            {
                skip = skipVariable.Name.Value;
            }
            else if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal)
                && directive.Arguments.Count == 1
                && directive.Arguments[0].Value is VariableNode includeVariable)
            {
                include = includeVariable.Name.Value;
            }

            if (skip is not null && include is not null)
            {
                includeCondition = new IncludeCondition(skip, include);
                return true;
            }
        }

        includeCondition = default;
        return false;
    }
}
