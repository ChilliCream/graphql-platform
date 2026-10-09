namespace Mocha;

internal static class MessagingSetupContextExtensions
{
    public static void BindRouteToEndpoint(
        this IMessagingSetupContext context,
        InboundRoute route,
        ReceiveEndpoint endpoint)
    {
        if (route.Endpoint is null)
        {
            route.ConnectEndpoint(context, endpoint);
            return;
        }

        if (route.Endpoint == endpoint)
        {
            return;
        }

        foreach (var existing in context.Router.GetInboundByEndpoint(endpoint))
        {
            if (existing.Consumer == route.Consumer
                && existing.Kind == route.Kind
                && existing.MessageType == route.MessageType)
            {
                return;
            }
        }

        var clone = new InboundRoute();
        clone.Initialize(context, new InboundRouteConfiguration
        {
            MessageType = route.MessageType,
            Consumer = route.Consumer,
            Kind = route.Kind,
            Condition = route.Condition
        });
        clone.ConnectEndpoint(context, endpoint);
    }
}
