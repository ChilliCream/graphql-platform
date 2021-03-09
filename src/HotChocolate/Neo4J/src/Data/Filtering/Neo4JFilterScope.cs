using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <inheritdoc />
    public class Neo4JFilterScope
        : FilterScope<Condition>
    {
        /// <summary>
        /// The path from the root to the current position in the input object
        /// </summary>
        public Stack<string> Path { get; } = new ();
    }
}
