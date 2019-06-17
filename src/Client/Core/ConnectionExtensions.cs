using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Client.Core;

namespace HotChocolate.Client
{
    public static class ConnectionExtensions
    {
        public static Task<T> Run<T>(
            this IConnection connection,
            IQueryableValue<T> expression,
            CancellationToken cancellationToken = default)
        {
            return connection.Run(expression.Compile(), cancellationToken: cancellationToken);
        }

        public static Task<IEnumerable<T>> Run<T>(
            this IConnection connection,
            IQueryableList<T> expression,
            CancellationToken cancellationToken = default)
        {
            return connection.Run(expression.Compile(), cancellationToken: cancellationToken);
        }

        public static async Task<T> Run<T>(
            this IConnection connection,
            ICompiledQuery<T> query,
            Dictionary<string, object> variables = null,
            CancellationToken cancellationToken = default)
        {
            var run = query.Start(connection, variables);
            while (await run.RunPage(cancellationToken).ConfigureAwait(false)) { }
            return run.Result;
        }
    }
}
