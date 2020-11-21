using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J
{
    public class CypherQuery
    {
        /// <summary>
        /// Gets if query is a read or write.
        /// </summary>
        public bool IsWrite { get; }

        /// <summary>
        /// Gets the query's text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the query's parameters.
        /// </summary>
        public IDictionary<string, object> Parameters { get; }

        /// <summary>
        /// Create a query
        /// </summary>
        /// <param name="text">The query's text</param>
        /// <param name="parameters">The query's parameters, whose values should not be changed while the query is used in a session/transaction.</param>
        /// <param name="isWrite">If the query will be a write or read.</param>
        public CypherQuery(
            string text,
            IDictionary<string, object> parameters,
            bool isWrite = false)
        {
            Text = text;
            Parameters = parameters ?? new Dictionary<string, object>();
            IsWrite = isWrite;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => Text;
    }
}
