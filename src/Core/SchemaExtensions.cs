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
            return schema.OperationExecuter.ExecuteRequestAsync(
                Parser.Default.Parse(query), null, null, null,
                CancellationToken.None);
        }

        public static Task<QueryResult> ExecuteAsync(
            this Schema schema, string query,
            string operationName = null,
            Dictionary<string, IValueNode> variableValues = null,
            object initialValue = null,
            CancellationToken cancellationToken = default)
        {
            return schema.OperationExecuter.ExecuteRequestAsync(
                Parser.Default.Parse(query), operationName, null, null,
                CancellationToken.None);
        }

        public static QueryResult Execute(this Schema schema, string query)
        {
            return Task.Factory.StartNew(
                () => schema.OperationExecuter.ExecuteRequestAsync(
                    Parser.Default.Parse(query), null, null, null,
                    CancellationToken.None))
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static QueryResult Execute(
            this Schema schema, string query,
            string operationName = null,
            Dictionary<string, IValueNode> variableValues = null,
            object initialValue = null,
            CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(
                () => schema.OperationExecuter.ExecuteRequestAsync(
                    Parser.Default.Parse(query), operationName, null, null,
                    CancellationToken.None))
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }
    }

}
