# NATS GraphQL Subscriptions backplane for HotChocolate

## What is NATS?

NATS is a simple, secure and high performance open source messaging system for cloud native applications, IoT messaging, and microservices architectures. It offers services for both asynchronous communication and streaming data.

## Why do I need NATS for my GraphQL Subscriptions?

If you scale your HotChocolate servers beyond a single node, some of your GraphQL Subscriptions will be handled by different servers. The subscriber may receive the subscription results on a different server than the publisher. In order to make this work, you need a backplane. Another example of a backplane is the Redis backplane that is included in HotChocolate. The Redis backplane is a good choice if you want to reuse some of your existing infrastructure. However, if you want to avoid the overhead of a Redis server, you can use the NATS backplane.

The NATS backplane is a simple and lightweight alternative to the Redis backplane. It is also typically faster than the Redis backplane, and cheaper, as you can run it on lightweight hardware, virtual or not, and scale it horizontally. The Publish/Subscribe pattern does not require a lot of memory, and Redis typically scales in a vertical fashion, adding more memory to a single server. NATS scales horizontally, adding more servers to the cluster.

## How do I use the NATS backplane?

You can start with a single node of NATS and see where you need to go from there. You can also start with a cluster of NATS servers and scale it. The NATS client uses a technique called client-side load balancing to distribute the load across the cluster. The NATS client will automatically discover the cluster and choose the correct server to publish to or subscribe from, based on a hash of the subject. The NATS client will also automatically reconnect to the cluster if a server goes down.

You do not need to enable persistence in the NATS server (JetStream) for Publish/Subscribe to function.

```csharp
using AlterNats;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddNats(poolSize: 1, opts => opts with
    {
        Url = "nats://localhost:4222",
        // Optional serializer (defaults to System.Text.Json)
        Serializer = new MessagePackNatsSerializer()
    })
    .AddNatsSubscriptions()
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

> **NOTE**:
> _You need to target net6.0 or higher to use this provider, as the NATS client dependency is not available for net5.0 or lower._ 

## Technical Information about NATS

NATS has a nice command line interface which you can use to monitor the cluster. You can also use the NATS dashboard to monitor the cluster.

### NATS PubSub overview using NATS CLI

https://www.youtube.com/watch?v=jLTVhP08Tq0
