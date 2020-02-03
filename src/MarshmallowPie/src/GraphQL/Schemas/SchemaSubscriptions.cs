using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using MarshmallowPie.Processing;

namespace MarshmallowPie.GraphQL.Schemas
{
    [ExtendObjectType(Name = "Subscription")]
    public class SchemaSubscriptions
    {
        [Subscribe]
        public ValueTask<IAsyncEnumerable<PublishDocumentEvent>> OnPublishSchema(
            string sessionId,
            [Service]ISessionMessageReceiver<PublishDocumentEvent> messageReceiver,
            CancellationToken cancellationToken) =>
            messageReceiver.SubscribeAsync(sessionId, cancellationToken);
    }
}
