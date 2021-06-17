using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.AspNetCore.Utilities
{
    [ExtendObjectType(OperationTypeNames.Subscription)]
    public class SubscriptionsExtensions
    {
        [SubscribeAndResolve]
        public async IAsyncEnumerable<string> OnNext(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                yield return "next";
                await Task.Delay(50, cancellationToken);
            }
        }
    }
}
