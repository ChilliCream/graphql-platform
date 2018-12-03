using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    public partial class QueryExecuter
    {
        private QueryInfo GetOrCreateQuery(string queryText)
        {
            if (_useCache)
            {
                string queryKey = NormalizeQuery(queryText);
                return _queryCache.GetOrCreate(queryKey,
                    () => CreateQueryInfo(queryText));
            }
            return CreateQueryInfo(queryText);
        }

        private QueryInfo CreateQueryInfo(string queryText)
        {
            DocumentNode queryDocument = _queryParser.Parse(queryText);
            QueryValidationResult validationResult =
                _queryValidator.Validate(queryDocument);
            return new QueryInfo(queryText, queryDocument, validationResult);
        }

        private string NormalizeQuery(string query)
        {
            return query
                .Replace("\r", string.Empty)
                .Replace("\n", " ");
        }

        private class QueryInfo
        {
            public QueryInfo(
                string queryText,
                DocumentNode queryDocument,
                QueryValidationResult validationResult)
            {
                QueryText = queryText
                    ?? throw new ArgumentNullException(nameof(queryText));
                QueryDocument = queryDocument
                    ?? throw new ArgumentNullException(nameof(queryDocument));
                ValidationResult = validationResult
                    ?? throw new ArgumentNullException(nameof(validationResult));
            }

            public string QueryText { get; }

            public DocumentNode QueryDocument { get; }

            public QueryValidationResult ValidationResult { get; }
        }
    }
}
