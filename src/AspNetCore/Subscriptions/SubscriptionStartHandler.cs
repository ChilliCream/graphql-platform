using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public sealed class SubscriptionStartHandler
        : IRequestHandler
    {
        public bool CanHandle(GenericOperationMessage message)
        {
            return message.Type == MessageTypes.Subscription.Start;
        }

        public async Task HandleAsync(
            IWebSocketContext context,
            GenericOperationMessage message,
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

            if (result is IResponseStream responseStream)
            {
                context.RegisterSubscription(
                    new Subscription(context, responseStream, message.Id));
            }
            else if (result is IQueryExecutionResult queryResult)
            {
                await context.SendSubscriptionDataMessageAsync(
                    message.Id, queryResult, cancellationToken);
                await context.SendSubscriptionCompleteMessageAsync(
                    message.Id, cancellationToken);
            }
        }
    }
}
