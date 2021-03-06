using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JAndFilterDefinition : Neo4JFilterDefinition
    {
        private readonly List<Neo4JFilterDefinition> _filters;

        public Neo4JAndFilterDefinition(IEnumerable<Neo4JFilterDefinition> filters)
        {
            _filters = filters.ToList();
        }
    }
}
