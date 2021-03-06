using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Neo4jDemo.Schema
{
    [ExtendObjectType(Name = "Subscription")]
    public class Subscriptions
    {
        [Subscribe(With = nameof(SubscribeToOnBusinessCreateAsync))]
        public bool OnBusinessCreate() => true;

        public async ValueTask<ISourceStream<int>> SubscribeToOnBusinessCreateAsync(
            int id,
            [Service] ITopicEventReceiver eventReceiver,
            CancellationToken cancellationToken) =>
            await eventReceiver.SubscribeAsync<string, int>(
                "OnBusinessCreate_" + id, cancellationToken);

    }
}
