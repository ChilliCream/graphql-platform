using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public interface IHasDirectives
    {
        IReadOnlyList<DirectiveNode> Directives { get; }
    }
}
