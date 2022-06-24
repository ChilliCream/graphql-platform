using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Types.Pipeline;

internal sealed class RemoveDirective
{
    public static bool TryParse(
        DirectiveNode syntax,
        [NotNullWhen(true)] out RemoveDirective? rename)
    {
        if (syntax.Name.Value.EqualsOrdinal("remove"))
        {
            rename = new RemoveDirective();
            return true;
        }

        rename = null;
        return false;
    }

    public static bool IsOfType(DirectiveNode syntax)
    {
        return syntax.Name.Value.EqualsOrdinal("remove");
    }

    public static implicit operator DirectiveNode(RemoveDirective rename)
        => new(
            null,
            new NameNode("remove"),
            Array.Empty<ArgumentNode>());
}
