using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;

namespace HotChocolate.Execution
{
    internal static class SourceStreamHelper
    {
        public static async IAsyncEnumerable<object> ToSourceStream(
            this IEventStream stream,
            [EnumeratorCancellation]CancellationToken cancellationToken)
        {
            await foreach (IEventMessage message in stream.WithCancellation(cancellationToken))
            {
                yield return message;
            }
        }
    }
}
