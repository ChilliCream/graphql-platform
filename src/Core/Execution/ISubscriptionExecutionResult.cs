using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public interface ISubscriptionExecutionResult
        : IExecutionResult
        , IDisposable
    {
        IQueryExecutionResult Current { get; }

        Task<bool> MoveNextAsync(CancellationToken cancellationToken = default);
    }

    public class Foo
    {
        public async Task Bar(ISubscriptionExecutionResult result)
        {
            using (result)
            {
                while (await result.MoveNextAsync())
                {

                }
            }
        }
    }
}
