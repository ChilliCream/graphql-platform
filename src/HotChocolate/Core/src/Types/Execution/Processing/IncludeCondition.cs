using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal readonly struct IncludeCondition : IEquatable<IncludeCondition>
{
    private readonly VariableNode? _skip;
    private readonly VariableNode? _include;

    public IncludeCondition(VariableNode? skip, VariableNode? include)
    {
        _skip = skip;
        _include = include;
    }

    public VariableNode? Skip => _skip;

    public VariableNode? Include => _include;

    public bool IsIncluded(IVariableValueCollection variableValues)
    {
        if (_skip is not null
            && DirectiveIfCondition.IsEnabled(DirectiveNames.Skip.Name, _skip, variableValues))
        {
            return false;
        }

        if (_include is not null
            && !DirectiveIfCondition.IsEnabled(DirectiveNames.Include.Name, _include, variableValues))
        {
            return false;
        }

        return true;
    }

    public bool Equals(IncludeCondition other)
        => string.Equals(Skip?.Name.Value, other.Skip?.Name.Value, StringComparison.Ordinal)
            && string.Equals(Include?.Name.Value, other.Include?.Name.Value, StringComparison.Ordinal);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is IncludeCondition other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Skip?.Name.Value, Include?.Name.Value);

    public static bool TryCreate(FieldNode field, out IncludeCondition includeCondition)
        => TryCreate(field.Directives, out includeCondition);

    public static bool TryCreate(InlineFragmentNode inlineFragment, out IncludeCondition includeCondition)
        => TryCreate(inlineFragment.Directives, out includeCondition);

    private static bool TryCreate(IReadOnlyList<DirectiveNode> directives, out IncludeCondition includeCondition)
    {
        VariableNode? skip = null;
        VariableNode? include = null;

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

    private static void TryParseDirective(DirectiveNode directive, ref VariableNode? skip, ref VariableNode? include)
    {
        if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal)
            && directive.Arguments.Count == 1
            && directive.Arguments[0].Value is VariableNode skipVariable)
        {
            skip = skipVariable;
        }
        else if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal)
            && directive.Arguments.Count == 1
            && directive.Arguments[0].Value is VariableNode includeVariable)
        {
            include = includeVariable;
        }
    }
}
