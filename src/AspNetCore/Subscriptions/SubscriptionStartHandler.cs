using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public sealed class SubscriptionStartHandler
        : IRequestHandler
    {
        public bool CanHandle(OperationMessage message)
        {
            return message.Type == MessageTypes.Subscription.Start;
        }

        public async Task HandleAsync(
            IWebSocketContext context,
            OperationMessage message,
            CancellationToken cancellationToken)
        {
            QueryRequest request = message.Payload.ToObject<QueryRequest>();

            IExecutionResult result = await context.QueryExecuter.ExecuteAsync(
                new Execution.QueryRequest(request.Query, request.OperationName)
                {
                    VariableValues = QueryMiddlewareUtilities
                        .DeserializeVariables(request.Variables),
                    Services = QueryMiddlewareUtilities
                        .CreateRequestServices(context.HttpContext)
                },
                cancellationToken).ConfigureAwait(false);

            context.RegisterSubscription(
                new Subscription(context, (IResponseStream)result, message.Id));
        }
    }



}
