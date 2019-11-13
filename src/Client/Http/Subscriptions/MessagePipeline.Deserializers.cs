using System;
using HotChocolate.Language;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    internal partial class MessagePipeline
    {
        private OperationMessage DeserializeMessage(
            GraphQLSocketMessage parsedMessage)
        {
            switch (parsedMessage.Type)
            {
                // case MessageTypes.Connection.Error:

                case MessageTypes.Connection.Accept:
                    return AcceptConnectionMessage.Default;

                case MessageTypes.Subscription.Data:
                    return DeserializeSubscriptionResultMessage(parsedMessage);

                // case MessageTypes.Subscription.Error:

                case MessageTypes.Subscription.Complete:
                    return DeserializeSubscriptionCompleteMessage(parsedMessage);

                default:
                    return KeepConnectionAliveMessage.Default;
            }
        }

        private static DataCompleteMessage DeserializeSubscriptionCompleteMessage(
            GraphQLSocketMessage parsedMessage)
        {
            if (parsedMessage.Id is null)
            {
                // TODO : resources
                throw new InvalidOperationException("Invalid message structure.");
            }
            return new DataCompleteMessage(parsedMessage.Id);
        }

        private OperationMessage DeserializeSubscriptionResultMessage (
            GraphQLSocketMessage parsedMessage)
        {
            if (parsedMessage.Id is null || !parsedMessage.HasPayload)
            {
                // TODO : resources
                throw new InvalidOperationException("Invalid message structure.");
            }

            if (_subscriptionManager.TryGetSubscription(
                parsedMessage.Id,
                out ISubscription subscription))
            {
                IResultParser parser = subscription.ResultParser;
                OperationResultBuilder resultBuilder =
                        OperationResultBuilder.New(parser.ResultType);
                parser.Parse(parsedMessage.Payload, resultBuilder);
                return new DataResultMessage(parsedMessage.Id, resultBuilder);
            }

            return KeepConnectionAliveMessage.Default;
        }
    }
}
