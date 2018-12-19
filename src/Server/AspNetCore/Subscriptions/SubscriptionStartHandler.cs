#if !ASPNETCLASSIC

using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class SubscriptionStartHandler
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
            QueryRequestDto payload = message.Payload.ToObject<QueryRequestDto>();

            var request = new QueryRequest(payload.Query, payload.OperationName)
            {
                VariableValues = QueryMiddlewareUtilities
                    .ToDictionary(payload.Variables),
                Services = context.HttpContext.CreateRequestServices()
            };

            await context.PrepareRequestAsync(request)
                .ConfigureAwait(false);

            IExecutionResult result =
                await context.QueryExecuter.ExecuteAsync(
                    request, cancellationToken)
                    .ConfigureAwait(false);

            if (result is IResponseStream responseStream)
            {
                context.RegisterSubscription(
                    new Subscription(context, responseStream, message.Id));
            }
            else if (result is IQueryExecutionResult queryResult)
            {
                await context.SendSubscriptionDataMessageAsync(
                    message.Id, queryResult, cancellationToken)
                    .ConfigureAwait(false);
                await context.SendSubscriptionCompleteMessageAsync(
                    message.Id, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}

#endif
