# RabbitMQ GraphQL Subscriptions backplane for HotChocolate

## What is RabbitMQ?

RabbitMQ is the most widely deployed open source message broker. RabbitMQ is lightweight and easy to deploy on premises and in the cloud. It supports multiple messaging protocols. RabbitMQ can be deployed in distributed and federated configurations to meet high-scale, high-availability requirements.

Find out more [here](https://rabbitmq.com).

## Why do I need RabbitMQ for my GraphQL Subscriptions?

If you scale your HotChocolate servers beyond a single node, some of your GraphQL Subscriptions will be handled by different servers. The subscriber may receive the subscription results on a different server than the publisher. In order to make this work, you need a backplane. Another example of a backplane is the Redis backplane that is included in HotChocolate. The Redis backplane is a good choice if you want to reuse some of your existing infrastructure. However, if you want to avoid the overhead of a Redis server, you can use the RabbitMQ backplane.

The RabbitMQ backplane is a simple and lightweight alternative to the Redis backplane. It is also typically faster than the Redis backplane, and cheaper, as you can run it on lightweight hardware, virtual or not, and scale it horizontally. The Publish/Subscribe pattern does not require a lot of memory, and Redis typically scales in a vertical fashion, adding more memory to a single server. RabbitMQ scales horizontally, adding more servers to the cluster.

## How do I use the RabbitMQ backplane?

You can start with a single node of RabbitMQ and see where you need to go from there. You can also start with a cluster of RabbitMQ servers and scale it.

```csharp
using HotChocolate.Execution;
using HotChocolate.Subscriptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRabbitMQSubscriptions()
    .AddGraphQLServer()
    .AddMutationConventions()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>();

var app = builder.Build();

app.UseWebSockets();
app.UseRouting();
app.UseEndpoints(ep =>
{
    ep.MapGraphQL();
});

await app.RunAsync();
```

If you need to override the default RabbitMQ configuration, you can provide your own instance of the `ConnectionFactory`. See the following example.

```csharp
AddRabbitMQSubscriptions(new()
{
    HostName = "123.456.1.1",
    Port = 1234,
});
```

**NOTE**
_Even so you can set the two properties `AutomaticRecoveryEnabled` and `DispatchConsumersAsync` by providing a custom `ConnectionFactory` instance, they both will be reset to `true` before creating a new connection._
