using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

internal readonly struct IncludeCondition : IEquatable<IncludeCondition>
{
    private readonly string? _skip;
    private readonly string? _include;

    public IncludeCondition(string? skip, string? include)
    {
        _skip = skip;
        _include = include;
    }

    public string? Skip => _skip;

    public string? Include => _include;

    public bool IsIncluded(IVariableValueCollection variableValues)
    {
        if (_skip is not null)
        {
            if (!variableValues.TryGetValue<BooleanValueNode>(_skip, out var value))
            {
                throw new InvalidOperationException($"The variable {_skip} has an invalid value.");
            }

            if (value.Value)
            {
                return false;
            }
        }

        if (_include is not null)
        {
            if (!variableValues.TryGetValue<BooleanValueNode>(_include, out var value))
            {
                throw new InvalidOperationException($"The variable {_include} has an invalid value.");
            }

            if (!value.Value)
            {
                return false;
            }
        }

        return true;
    }

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
