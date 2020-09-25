using System.Collections.Generic;

namespace HotChocolate.Data.Neo4j.Cypher
{
    public class CypherCommand
    {

        /// <summary>
        /// Representation of Cypher Query.
        /// </summary>
        public string Query { get; }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        public Dictionary<string, object?> Parameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CypherCommand"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="parameters">The parameters.</param>
        public CypherCommand(
            string query,
            Dictionary<string, object?> parameters)
        {
            Query = query;
            Parameters = parameters;
        }
    }
}
