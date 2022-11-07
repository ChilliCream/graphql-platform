using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Types.Pipeline;

internal sealed class BindDirective
{
    public BindDirective(string to, string? @as = null)
    {
        To = to;
        As = @as;
    }

    public string To { get; }

    public string? As { get; }

    public static bool TryParse(DirectiveNode syntax, [NotNullWhen(true)] out BindDirective? bind)
    {
        if (syntax.Name.Value.EqualsOrdinal("_hc_bind"))
        {
            if (syntax.Arguments.Count is <= 0 or > 2)
            {
                bind = null;
                return false;
            }

            string? toValue = null;
            string? asValue = null;

            foreach (ArgumentNode argument in syntax.Arguments)
            {
                switch (argument.Name.Value)
                {
                    case "to" when argument.Value is StringValueNode sv:
                        toValue = sv.Value;
                        break;

                    case "as" when argument.Value is StringValueNode sv:
                        asValue = sv.Value;
                        break;

                    case "as" when argument.Value is NullValueNode:
                        asValue = null;
                        break;

                    default:
                        bind = null;
                        return false;
                }
            }

            if (toValue is null)
            {
                bind = null;
                return false;
            }

            if (syntax.Arguments.Count is 2 && asValue is null)
            {
                bind = null;
                return false;
            }

            bind = new BindDirective(toValue, asValue);
            return false;
        }

        bind = null;
        return false;
    }

    public static bool IsOfType(DirectiveNode syntax)
    {
        if (syntax.Name.Value.EqualsOrdinal("_hc_bind"))
        {
            if (syntax.Arguments.Count is <= 0 or > 2)
            {
                return false;
            }

            bool hasTo = false, hasAs = false;

            foreach (ArgumentNode argument in syntax.Arguments)
            {
                if (argument.Name.Value.EqualsOrdinal("to") &&
                    argument.Value is StringValueNode { Value.Length: > 0 })
                {
                    hasTo = true;
                }
                else if (argument.Name.Value.EqualsOrdinal("as") &&
                    argument.Value is StringValueNode { Value.Length: > 0 })
                {
                    hasAs = true;
                }
                else
                {
                    return false;
                }
            }

            if (hasAs && hasTo)
            {
                return true;
            }

            if (hasTo)
            {
                return true;
            }

            return false;
        }

        return false;
    }

    public static implicit operator DirectiveNode(BindDirective bind)
    {
        ArgumentNode[] arguments;

        if (string.IsNullOrEmpty(bind.As))
        {
            arguments = new[]
            {
                new ArgumentNode("to", new StringValueNode(bind.To))
            };
        }
        else
        {
            arguments = new[]
            {
                new ArgumentNode("to", new StringValueNode(bind.To)),
                new ArgumentNode("as", new StringValueNode(bind.As))
            };
        }

        return new DirectiveNode(
            null,
            new NameNode("_hc_bind"),
            arguments);
    }
}
