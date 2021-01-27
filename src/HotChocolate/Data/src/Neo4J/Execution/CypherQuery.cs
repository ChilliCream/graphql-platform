namespace HotChocolate.Data.Neo4J
{
    public class CypherQuery<T>
    {
        /// <summary>
        /// Is the query is a read or write.
        /// </summary>
        private readonly bool _isWrite;

        /// <summary>
        /// The query's text.
        /// </summary>
        private readonly string _text;

        /// <summary>
        /// The query's parameters.
        /// </summary>
        private CypherQueryParameters _parameters;

        /// <summary>
        /// Create a query
        /// </summary>
        /// <param name="text">The query's text</param>
        /// <param name="parameters">The query's parameters, whose values should not be changed while the query is used in a session/transaction.</param>
        /// <param name="isWrite">If the query will be a write or read.</param>
        public CypherQuery(
            string text,
            CypherQueryParameters parameters,
            bool isWrite = false)
        {
            _text = text;
            _parameters = parameters ?? new CypherQueryParameters();
            _isWrite = isWrite;
        }

        public bool IsWrite() => _isWrite;

        public override string ToString() => _text;
    }
}
