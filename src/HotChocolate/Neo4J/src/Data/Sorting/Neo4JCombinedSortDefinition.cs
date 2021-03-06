using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Sorting
{
    internal sealed class Neo4JCombinedSortDefinition : Neo4JSortDefinition
    {
        private readonly Neo4JSortDefinition[] _sorts;

        public Neo4JCombinedSortDefinition(params Neo4JSortDefinition[] sorts)
        {
            _sorts = sorts;
        }

        public Neo4JCombinedSortDefinition(IEnumerable<Neo4JSortDefinition> sorts)
        {
            _sorts = Ensure.IsNotNull(sorts, nameof(sorts)).ToArray();
        }
    }
}
