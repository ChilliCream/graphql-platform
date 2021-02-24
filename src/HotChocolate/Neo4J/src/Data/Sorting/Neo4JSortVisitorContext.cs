using System.Collections.Generic;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.Neo4J.Sorting
{
    public class Neo4JSortVisitorContext : SortVisitorContext<Neo4JSortDefinition>
    {
        public Neo4JSortVisitorContext(ISortInputType initialType)
            : base(initialType)
        {
        }

        /// <summary>
        /// The path from the root to the current position in the input object
        /// </summary>
        public Stack<string> Path { get; } = new ();
    }
}
