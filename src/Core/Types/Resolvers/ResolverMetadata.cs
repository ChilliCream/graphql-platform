using System.Collections.Generic;

namespace HotChocolate.Resolvers
{
    public class ResolverMetadata
    {
        public IReadOnlyList<string> DependsOn { get; }

        public IReadOnlyList<string> Resources { get; }
    }
}
