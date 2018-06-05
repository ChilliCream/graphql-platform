using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate
{
    public static class SchemaExtensions
    {
        private static readonly OperationExecuter _operationExecuter = new OperationExecuter();

        public static Task<QueryResult> ExecuteAsync(this Schema schema, string query)
        {
            return _operationExecuter.ExecuteRequestAsync(schema,
                Parser.Default.Parse(query), null, null, null,
                CancellationToken.None);
        }

         public static Task<QueryResult> ExecuteAsync(
             this Schema schema, string query,
             string operationName = null,
             CancellationToken cancellationToken = default)
        {
            return _operationExecuter.ExecuteRequestAsync(schema,
                Parser.Default.Parse(query), operationName, null, null,
                CancellationToken.None);
        }
    }

}
