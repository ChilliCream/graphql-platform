using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Types.Pipeline;

internal sealed class RenameDirective
{
    public RenameDirective(string to)
    {
        To = to;
    }

    public string To { get; }

    public static bool TryParse(
        DirectiveNode syntax,
        [NotNullWhen(true)] out RenameDirective? rename)
    {
        if (syntax.Name.Value.EqualsOrdinal("rename"))
        {
            if (syntax.Arguments.Count == 1)
            {
                ArgumentNode argument = syntax.Arguments[0];
                if (argument.Name.Value.EqualsOrdinal("to") &&
                    argument.Value is StringValueNode sv &&
                    sv.Value.Length > 0)
                {
                    rename = new RenameDirective(sv.Value);
                    return true;
                }
            }
        }

        rename = null;
        return false;
    }

    public static bool IsOfType(DirectiveNode syntax)
    {
        if (syntax.Name.Value.EqualsOrdinal("rename"))
        {
            if (syntax.Arguments.Count == 1)
            {
                ArgumentNode argument = syntax.Arguments[0];
                if (argument.Name.Value.EqualsOrdinal("to"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static implicit operator DirectiveNode(RenameDirective rename)
        => new DirectiveNode(
            null,
            new NameNode("rename"),
            new ArgumentNode[1]
            {
                new(null, new NameNode(null, "to"), new StringValueNode(rename.To))
            });
}
