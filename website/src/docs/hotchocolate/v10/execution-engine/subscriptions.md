---
title: Subscriptions
---

# What are GraphQL subscriptions?

Subscriptions is a GraphQL feature that allows a server to send data to its clients when a specific event on the server-side occurs.

Subscribing to an event is like writing a standard query. The one difference here is the operation keyword and that we are only allowed to have one root field in our query since the root fields represent events.

```graphql
subscription {
  onReview(episode: NEWHOPE) {
    stars
    comment
  }
}
```

When using GraphQL over HTTP subscriptions are most most likely served over websocket. Hot Chocolate has implemented the Apollo subscriptions protocol in order to serve subscriptions over websocket.

# Getting started

The subscription type is almost implemented like a simple query. In many cases subscriptions are raised through mutations, but subscriptions could also be raised through other backend systems.

In order to enable subscriptions we have to register a subscription provider with our server. A subscription provider represents a pub/sub system abstraction that handles the events.

We currently support the following subscription provider:

- InMemory
  This one is good enough if we have a single server and all events are triggered through our mutations.

- Redis
  We have an out-of-the-box redis subscription provider that uses the redis publish/subscribe functionality. If we have multiple instances of our server then this provider is our best option.

> We are in the process to add more pub-/sub-provider for Kafka, Redis Streams, Azure EventHub and Azure ServiceBus. We also can help along if you want to implement your own subscription provider.

In order to add the subscription provider to our server add the following service in the `ConfigureServices` method of our `Startup.cs`:

```csharp
services.AddInMemorySubscriptionProvider();
```

or

```csharp
services.AddRedisSubscriptionProvider(configuration);
```

Finally, we have to configure our ASP.NET Core pipeline to use websocket:

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        app.UseWebSockets()
          .UseGraphQL();
    }
}
```

> More about configuring ASP.NET Core can be found [here](/docs/hotchocolate/v10/server).

Once this is setup, subscriptions are generally available. In order to define subscriptions we have to create a subscription type. The subscription type is just a regular `ObjectType`, so we create it like any other root type.

```csharp
public class SubscriptionType
    : ObjectType<Subscription>
{
    protected override void Configure(IObjectTypeDescriptor<Subscription> descriptor)
    {
        descriptor.Field(t => t.OnReview(default, default))
            .Type<NonNullType<ReviewType>>()
            .Argument("episode", arg => arg.Type<NonNullType<EpisodeType>>());
    }
}
```

But there is a difference when it comes to the resolver. A subscription resolver can ask for an additional argument that represents the event message.

```csharp
public Review OnReview(Episode episode, IEventMessage message)
{
    return (Review)message.Payload;
}
```

The event message can have a user-defined payload representing some kind of prepared data or whatever we want to put in there. The allowed payload size depends on the subscription provider.

The payload can also be null and we can pull relevant data in from other data sources whenever the event occurs.

An event is triggered when we use the `IEventSender` to raise an event. This will be mostly done within a mutation since the mutation represents the operation that changes the server state and hence cause it to raise events.

So, in our mutation we can ask for the `IEventSender` and raise an event like the following:

```csharp
public async Task<Review> CreateReview(
    Episode episode, Review review,
    [Service]IEventSender eventSender)
{
    _repository.AddReview(episode, review);
    await eventSender.SendAsync(new OnReviewMessage(episode, review));
    return review;
}
```

In the above case we are sending a `OnReviewMessage` which actually inherits from `EventMessage`.

```csharp
public class OnReviewMessage
    : EventMessage
{
    public OnReviewMessage(Episode episode, Review review)
        : base(CreateEventDescription(episode), review)
    {
    }

    private static EventDescription CreateEventDescription(Episode episode)
    {
        return new EventDescription("onReview",
            new ArgumentNode("episode",
                new EnumValueNode(
                    episode.ToString().ToUpperInvariant())));
    }
}
```

> We have a working example for subscription in our Star Wars [example](https://github.com/ChilliCream/hotchocolate/tree/master/examples/AspNetCore.StarWars).

# In-Memory Provider

The in-memory subscription provider does not need any configuration and is easily setup:

```csharp
services.AddInMemorySubscriptionProvider();
```

# Redis Provider

The redis subscription provider uses Redis as pub/sub system to handle messages, this enables us to run multiple instances of the Hot Chocolate server and handle subscription events reliably.

In order to use the Redis provider add the following package:
`HotChocolate.Subscriptions.Redis`

After we have added the package we can add the redis subscription provider to our services like the following:

```csharp
var configuration = new ConfigurationOptions
{
    Ssl = true,
    AbortOnConnectFail = false,
    Password = password
};

configuration.EndPoints.Add("host:port");

services.AddRedisSubscriptionProvider(configuration);
```

Our Redis subscription provider uses the `StackExchange.Redis` Redis client underneath and we have integration tests against the Azure Cache.

# PubSub usage from version 10.x

Since version `10.x` it is possible to create subscriptions in a more convenient fashion using the built-in PubSub System. The setup to get subscriptions running did not change. We may not need to use a specific Subscription Type. The Schema Builder can defer the Subscription Type directly from our method.

To use the PubSub System in the subscription inject is as a service via the `ITopicEventReceiver` and return the subscribed stream.

```csharp
public class Subscription
{
    [SubscribeAndResolve]
    public async ValueTask<IAsyncEnumerable<Review>> UserCreated(Episode episode, [Service] ITopicEventReceiver receiver)
    {
        return await receiver.SubscribeAsync<string, Review>($"Episode{episode}Reviewed");
    }

    // ...
}
```

> Make sure to use the `IAsyncEnumerable<T>` in the return value, otherwise the Schema Builder will throw an exception.

To trigger the subscription we can use the counterpart to the `ITopicEventReceiver`, which is the `ITopicEventSender`. It provides the `SendAsync` method, which takes two parameters:

- The first parameter is the _topic_ the PubSub should publish on.
- The second parameter is the _payload_.

In our case the review will published on the `EpisodeXYReviewed` channel. This call will subscriptions returned in our `Subscription` class above.

```csharp
public class Mutation {
    // ...

    public async Task<Review> CreateReview(
        Episode episode, Review review,
        [Service] ITopicEventSender eventSender)
    {
        _repository.AddReview(episode, review);
        await eventSender.SendAsync($"Episode{episode}Reviewed", review));
        return review;
    }

    // ...
}
```
