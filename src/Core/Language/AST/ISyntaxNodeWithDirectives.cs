using System.Collections.Generic;

namespace HotChocolate.Language
{
    public interface IHasDirectives
    {
        IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}
