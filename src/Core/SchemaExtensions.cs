using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate
{
    public static class SchemaExtensions
    {
        public static Task<QueryResult> ExecuteAsync(this Schema schema, string query)
        {
            return ExecuteAsync(schema,
                query, null, null, null,
                CancellationToken.None);
        }

        public static Task<QueryResult> ExecuteAsync(
            this Schema schema, string query,
            string operationName = null,
            Dictionary<string, IValueNode> variableValues = null,
            object initialValue = null,
            CancellationToken cancellationToken = default)
        {
            OperationRequest request = new OperationRequest(
                schema, Parser.Default.Parse(query), operationName);

            return request.ExecuteAsync(
                variableValues, initialValue,
                cancellationToken);
        }

        public static QueryResult Execute(this Schema schema, string query)
        {
            return Execute(schema, query,
                null, null, null,
                CancellationToken.None);
        }

        public static QueryResult Execute(
            this Schema schema, string query,
            string operationName = null,
            Dictionary<string, IValueNode> variableValues = null,
            object initialValue = null,
            CancellationToken cancellationToken = default)
        {
            OperationRequest request = new OperationRequest(
                schema, Parser.Default.Parse(query), operationName);

            return Task.Factory.StartNew(
                () => request.ExecuteAsync(
                    variableValues, initialValue,
                    cancellationToken))
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }
    }

}
