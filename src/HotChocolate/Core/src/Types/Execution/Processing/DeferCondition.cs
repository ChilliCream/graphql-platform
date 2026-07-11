using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal readonly struct DeferCondition(string? ifVariableName) : IEquatable<DeferCondition>
{
    public string? IfVariableName => ifVariableName;

    public bool IsDeferred(IVariableValueCollection variableValues)
    {
        if (ifVariableName is not null)
        {
            if (!variableValues.TryGetValue<BooleanValueNode>(ifVariableName, out var value))
            {
                throw new InvalidOperationException($"The variable {ifVariableName} has an invalid value.");
            }

            if (!value.Value)
            {
                return false;
            }
        }

        return true;
    }

    public bool Equals(DeferCondition other)
        => string.Equals(ifVariableName, other.IfVariableName, StringComparison.Ordinal);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is DeferCondition other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(ifVariableName);

    public static bool TryCreate(InlineFragmentNode inlineFragment, out DeferCondition deferCondition)
        => TryCreate(inlineFragment.Directives, out deferCondition);

    public static bool TryCreate(FragmentSpreadNode fragmentSpread, out DeferCondition deferCondition)
        => TryCreate(fragmentSpread.Directives, out deferCondition);

    private static bool TryCreate(IReadOnlyList<DirectiveNode> directives, out DeferCondition deferCondition)
    {
        if (directives.Count == 0)
        {
            deferCondition = default;
            return false;
        }

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];

            if (!directive.Name.Value.Equals(DirectiveNames.Defer.Name, StringComparison.Ordinal))
            {
                continue;
            }

            // @defer with no arguments is unconditionally deferred.
            if (directive.Arguments.Count == 0)
            {
                deferCondition = new DeferCondition(null);
                return true;
            }

            for (var j = 0; j < directive.Arguments.Count; j++)
            {
                var argument = directive.Arguments[j];

                if (!argument.Name.Value.Equals(DirectiveNames.Defer.Arguments.If, StringComparison.Ordinal))
                {
                    continue;
                }

                switch (argument.Value)
                {
                    // @defer(if: $variable) - conditionally deferred at runtime.
                    case VariableNode variable:
                        deferCondition = new DeferCondition(variable.Name.Value);
                        return true;

                    // @defer(if: true) - unconditionally deferred.
                    case BooleanValueNode { Value: true }:
                        deferCondition = new DeferCondition(null);
                        return true;

                    // @defer(if: false) - statically not deferred, no condition needed.
                    case BooleanValueNode { Value: false }:
                        deferCondition = default;
                        return false;
                }
            }

            // @defer directive found but no `if` argument matched - unconditionally deferred.
            deferCondition = new DeferCondition(null);
            return true;
        }

        deferCondition = default;
        return false;
    }
}
