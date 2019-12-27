using System.Collections.Generic;

namespace HotChocolate.Language
{
    public interface IHasDirectives
    {
        IReadOnlyList<DirectiveNode> Directives { get; }
    }
}
