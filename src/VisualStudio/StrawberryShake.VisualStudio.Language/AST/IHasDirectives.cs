using System.Collections.Generic;

namespace StrawberryShake.Language
{
    public interface IHasDirectives
    {
        IReadOnlyList<DirectiveNode> Directives { get; }
    }
}
