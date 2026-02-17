using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

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
        => TryCreate(field.Directives, out includeCondition);

    public static bool TryCreate(InlineFragmentNode inlineFragment, out IncludeCondition includeCondition)
        => TryCreate(inlineFragment.Directives, out includeCondition);

    private static bool TryCreate(IReadOnlyList<DirectiveNode> directives, out IncludeCondition includeCondition)
    {
        string? skip = null;
        string? include = null;

        if (directives.Count == 0)
        {
            includeCondition = default;
            return false;
        }

        if (directives.Count == 1)
        {
            TryParseDirective(directives[0], ref skip, ref include);
            if (TryCreateIncludeCondition(out includeCondition))
            {
                return true;
            }
        }

        if (directives.Count == 2)
        {
            TryParseDirective(directives[0], ref skip, ref include);
            TryParseDirective(directives[1], ref skip, ref include);
            return TryCreateIncludeCondition(out includeCondition);
        }

        if (directives.Count == 3)
        {
            TryParseDirective(directives[0], ref skip, ref include);
            TryParseDirective(directives[1], ref skip, ref include);

            if (skip is not null && include is not null)
            {
                includeCondition = new IncludeCondition(skip, include);
                return true;
            }

            TryParseDirective(directives[2], ref skip, ref include);
            return TryCreateIncludeCondition(out includeCondition);
        }

        for (var i = 0; i < directives.Count; i++)
        {
            TryParseDirective(directives[i], ref skip, ref include);

            if (skip is not null && include is not null)
            {
                includeCondition = new IncludeCondition(skip, include);
                return true;
            }
        }

        includeCondition = default;
        return false;

        bool TryCreateIncludeCondition(out IncludeCondition includeCondition)
        {
            if (skip is not null || include is not null)
            {
                includeCondition = new IncludeCondition(skip, include);
                return true;
            }

            includeCondition = default;
            return false;
        }
    }

    private static void TryParseDirective(DirectiveNode directive, ref string? skip, ref string? include)
    {
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
    }
}
