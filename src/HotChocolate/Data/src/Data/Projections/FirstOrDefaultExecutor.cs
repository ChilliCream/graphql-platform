using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Data.Projections
{
    internal static class FirstOrDefaultExecutor
    {
        public static async Task<object?> ExecuteAsync<T>(
            object? result,
            CancellationToken ct)
        {
            if (result is IAsyncEnumerable<T> ae)
            {
                await using IAsyncEnumerator<T> enumerator = ae.GetAsyncEnumerator(ct);

                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    return enumerator.Current;
                }

                return default(T);
            }

            if (result is IEnumerable<T> e)
            {
                return (await Task
                    .Run(() => e.FirstOrDefault(), ct)
                    .ConfigureAwait(false))!;
            }

            return result;
        }
    }
}
