using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddSubscriptionsDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "subscriptions-basic",
                Title = "Implementing GraphQL Subscriptions",
                Category = BestPracticeCategory.Subscriptions,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "subscription realtime real-time push websocket event notification live stream update",
                Abstract =
                    "How to define subscription types, implement event streams, and expose them via WebSocket using Hot Chocolate's subscription support.",
                Body = """
                # Implementing GraphQL Subscriptions

                ## When to Use

                Use subscriptions when clients need real-time updates pushed from the server. Common use cases include:

                - Chat messages and notifications
                - Live dashboards and monitoring
                - Order status updates
                - Collaborative editing indicators

                Subscriptions use WebSocket connections to maintain a persistent channel between client and server, delivering events as they occur.

                ## Implementation

                ### Define a Subscription Type

                ```csharp
                [SubscriptionType]
                public static class OrderSubscriptions
                {
                    [Subscribe]
                    [Topic]
                    public static Order OnOrderCreated(
                        [EventMessage] Order order)
                    {
                        return order;
                    }

                    [Subscribe]
                    [Topic]
                    public static Order OnOrderStatusChanged(
                        [EventMessage] Order order)
                    {
                        return order;
                    }
                }
                ```

                ### Publish Events from Mutations

                ```csharp
                [MutationType]
                public static class OrderMutations
                {
                    public static async Task<Order> CreateOrderAsync(
                        CreateOrderInput input,
                        AppDbContext dbContext,
                        [Service] ITopicEventSender eventSender,
                        CancellationToken cancellationToken)
                    {
                        var order = new Order
                        {
                            CustomerId = input.CustomerId,
                            Status = OrderStatus.Pending,
                            CreatedAt = DateTime.UtcNow
                        };

                        dbContext.Orders.Add(order);
                        await dbContext.SaveChangesAsync(cancellationToken);

                        await eventSender.SendAsync(
                            nameof(OrderSubscriptions.OnOrderCreated),
                            order,
                            cancellationToken);

                        return order;
                    }

                    public static async Task<Order> UpdateOrderStatusAsync(
                        int orderId,
                        OrderStatus newStatus,
                        AppDbContext dbContext,
                        [Service] ITopicEventSender eventSender,
                        CancellationToken cancellationToken)
                    {
                        var order = await dbContext.Orders.FindAsync(orderId);
                        order!.Status = newStatus;
                        await dbContext.SaveChangesAsync(cancellationToken);

                        await eventSender.SendAsync(
                            nameof(OrderSubscriptions.OnOrderStatusChanged),
                            order,
                            cancellationToken);

                        return order;
                    }
                }
                ```

                ### Configure WebSocket Support

                ```csharp
                var builder = WebApplication.CreateBuilder(args);

                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddMutationType()
                    .AddSubscriptionType()
                    .AddTypes()
                    .AddInMemorySubscriptions();

                var app = builder.Build();

                app.UseWebSockets();
                app.MapGraphQL();

                app.Run();
                ```

                ### Client Subscription

                ```graphql
                subscription {
                  onOrderCreated {
                    id
                    customerId
                    status
                    createdAt
                  }
                }
                ```

                ## Anti-patterns

                **Publishing events without await:**

                ```csharp
                // BAD: Fire-and-forget can lose events and hide errors
                public static async Task<Order> CreateOrderAsync(
                    CreateOrderInput input,
                    AppDbContext dbContext,
                    [Service] ITopicEventSender eventSender,
                    CancellationToken cancellationToken)
                {
                    var order = new Order { /* ... */ };
                    await dbContext.SaveChangesAsync(cancellationToken);

                    _ = eventSender.SendAsync(  // Fire-and-forget!
                        nameof(OrderSubscriptions.OnOrderCreated),
                        order,
                        cancellationToken);

                    return order;
                }
                ```

                **Forgetting UseWebSockets:**

                ```csharp
                // BAD: Without UseWebSockets, subscription connections will fail
                var app = builder.Build();
                // app.UseWebSockets(); // Missing!
                app.MapGraphQL();
                ```

                **Sending too much data in events:**

                ```csharp
                // BAD: Sending the entire aggregate with all relations
                await eventSender.SendAsync(
                    "OnOrderCreated",
                    orderWithAllItemsAndCustomerAndPayments, // Huge payload
                    cancellationToken);
                // Send just the entity or its ID — let the subscription resolver fetch what's needed
                ```

                ## Key Points

                - Use `[SubscriptionType]` to define subscription root fields
                - Use `[Subscribe]` and `[Topic]` attributes to declare event subscriptions
                - Publish events from mutations using `ITopicEventSender.SendAsync`
                - Call `AddInMemorySubscriptions()` for single-instance deployments
                - Always call `app.UseWebSockets()` before `app.MapGraphQL()` in the middleware pipeline
                - Keep event payloads small — the subscription resolver can fetch additional data as needed

                ## Related Practices

                - [subscriptions-filtered] — For filtered subscriptions
                - [subscriptions-providers] — For distributed subscription providers
                - [resolvers-field] — For resolver patterns used in subscriptions
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "subscriptions-filtered",
                Title = "Filtered Subscriptions",
                Category = BestPracticeCategory.Subscriptions,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "subscription filter topic channel selective subscribe event filtering targeted",
                Abstract =
                    "How to implement filtered subscriptions using [Subscribe] and [Topic] attributes, filtering events by arguments before they reach the client.",
                Body = """
                # Filtered Subscriptions

                ## When to Use

                Use filtered subscriptions when clients only want to receive events that match specific criteria. Instead of receiving all events and filtering on the client side, the server filters events before delivery, reducing bandwidth and client processing.

                Common use cases include:
                - Subscribing to status changes for a specific order
                - Receiving messages only for a specific chat room
                - Monitoring events for a specific user or tenant

                ## Implementation

                ### Topic-Based Filtering

                Use dynamic topics derived from subscription arguments:

                ```csharp
                [SubscriptionType]
                public static class ChatSubscriptions
                {
                    [Subscribe]
                    [Topic("{roomId}")]
                    public static ChatMessage OnMessageReceived(
                        [EventMessage] ChatMessage message,
                        string roomId)
                    {
                        return message;
                    }
                }
                ```

                Publish to the specific topic:

                ```csharp
                [MutationType]
                public static class ChatMutations
                {
                    public static async Task<ChatMessage> SendMessageAsync(
                        SendMessageInput input,
                        AppDbContext dbContext,
                        [Service] ITopicEventSender eventSender,
                        CancellationToken cancellationToken)
                    {
                        var message = new ChatMessage
                        {
                            RoomId = input.RoomId,
                            UserId = input.UserId,
                            Content = input.Content,
                            SentAt = DateTime.UtcNow
                        };

                        dbContext.Messages.Add(message);
                        await dbContext.SaveChangesAsync(cancellationToken);

                        // Publish to room-specific topic
                        await eventSender.SendAsync(
                            input.RoomId,
                            message,
                            cancellationToken);

                        return message;
                    }
                }
                ```

                ### Multi-Parameter Topics

                ```csharp
                [SubscriptionType]
                public static class NotificationSubscriptions
                {
                    [Subscribe]
                    [Topic("{userId}_{notificationType}")]
                    public static Notification OnNotification(
                        [EventMessage] Notification notification,
                        string userId,
                        NotificationType notificationType)
                    {
                        return notification;
                    }
                }
                ```

                ### Custom Topic Resolution

                For complex topic logic, use a method to resolve the topic:

                ```csharp
                [SubscriptionType]
                public static class OrderSubscriptions
                {
                    [Subscribe(With = nameof(SubscribeToOrderUpdates))]
                    public static OrderUpdate OnOrderUpdated(
                        [EventMessage] OrderUpdate update,
                        int orderId)
                    {
                        return update;
                    }

                    private static ValueTask<ISourceStream<OrderUpdate>> SubscribeToOrderUpdates(
                        int orderId,
                        [Service] ITopicEventReceiver receiver,
                        CancellationToken cancellationToken)
                    {
                        var topic = $"order_{orderId}_updates";
                        return receiver.SubscribeAsync<OrderUpdate>(topic, cancellationToken);
                    }
                }
                ```

                ### Client Queries

                ```graphql
                # Subscribe to messages in a specific room
                subscription {
                  onMessageReceived(roomId: "general") {
                    userId
                    content
                    sentAt
                  }
                }

                # Subscribe to order updates for a specific order
                subscription {
                  onOrderUpdated(orderId: 42) {
                    status
                    updatedAt
                  }
                }
                ```

                ## Anti-patterns

                **Filtering on the client after receiving all events:**

                ```csharp
                // BAD: Server sends ALL order events, client discards most of them
                [SubscriptionType]
                public static class OrderSubscriptions
                {
                    [Subscribe]
                    [Topic]
                    public static Order OnOrderUpdated([EventMessage] Order order)
                    {
                        return order; // Every client gets every order update
                    }
                }
                ```

                **Hardcoding topic names:**

                ```csharp
                // BAD: Hardcoded topic name means all subscribers get the same events
                await eventSender.SendAsync(
                    "order-updates",  // Same topic for all orders
                    order,
                    cancellationToken);
                ```

                **Complex filtering logic in the subscription resolver:**

                ```csharp
                // BAD: DB queries in subscription resolver run for every event
                [Subscribe]
                [Topic]
                public static async Task<Order?> OnOrderUpdated(
                    [EventMessage] Order order,
                    int customerId,
                    AppDbContext dbContext)
                {
                    var customer = await dbContext.Customers.FindAsync(customerId);
                    if (order.CustomerId != customer?.Id) return null; // Filter after receive
                    return order;
                }
                // Use topic-based filtering to avoid receiving irrelevant events entirely
                ```

                ## Key Points

                - Use `[Topic("{argumentName}")]` to create argument-derived topics for filtering
                - Each unique topic value creates a separate event stream
                - Publish events to the specific topic string that matches the subscription arguments
                - For complex topic logic, use `[Subscribe(With = nameof(Method))]` with a custom source stream
                - Topic-based filtering is more efficient than receiving all events and discarding unwanted ones
                - Keep topic granularity appropriate — too many unique topics can increase memory usage

                ## Related Practices

                - [subscriptions-basic] — For basic subscription setup
                - [subscriptions-providers] — For distributed subscription providers
                - [security-authorization] — For authorized subscriptions
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "subscriptions-providers",
                Title = "Subscription Providers: In-Memory vs Distributed",
                Category = BestPracticeCategory.Subscriptions,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "subscription provider redis in-memory message broker pub sub backend transport",
                Abstract =
                    "How to choose and configure subscription event providers: the default in-memory provider vs Redis-backed distributed providers for multi-instance deployments.",
                Body = """
                # Subscription Providers: In-Memory vs Distributed

                ## When to Use

                Choose a subscription provider based on your deployment model:

                - **In-Memory**: Single server instance, development, or testing. Events are dispatched within the same process. Simple to set up, no external dependencies.
                - **Redis**: Multiple server instances behind a load balancer. Events are published through Redis Pub/Sub, reaching all connected instances. Required for horizontal scaling.

                If your application runs as a single instance (most development scenarios and small deployments), in-memory is sufficient. If you deploy multiple instances, you must use a distributed provider or clients connected to different instances will miss events.

                ## Implementation

                ### In-Memory Provider (Default)

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddMutationType()
                    .AddSubscriptionType()
                    .AddTypes()
                    .AddInMemorySubscriptions();
                ```

                This is appropriate for:
                - Local development
                - Single-instance deployments
                - Integration testing

                ### Redis Provider

                Install the Redis subscription package:

                ```xml
                <PackageReference Include="HotChocolate.Subscriptions.Redis" Version="16.*" />
                ```

                Configure with a Redis connection:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddMutationType()
                    .AddSubscriptionType()
                    .AddTypes()
                    .AddRedisSubscriptions(sp =>
                        ConnectionMultiplexer.Connect(
                            builder.Configuration.GetConnectionString("Redis")!));
                ```

                ### Configuration Pattern for Environment

                ```csharp
                var graphql = builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddMutationType()
                    .AddSubscriptionType()
                    .AddTypes();

                if (builder.Environment.IsDevelopment())
                {
                    graphql.AddInMemorySubscriptions();
                }
                else
                {
                    graphql.AddRedisSubscriptions(sp =>
                        ConnectionMultiplexer.Connect(
                            builder.Configuration.GetConnectionString("Redis")!));
                }
                ```

                ### WebSocket Configuration

                Both providers require WebSocket middleware:

                ```csharp
                var app = builder.Build();

                app.UseWebSockets();
                app.MapGraphQL();

                app.Run();
                ```

                ### Health Check for Redis Subscriptions

                ```csharp
                builder.Services.AddHealthChecks()
                    .AddRedis(
                        builder.Configuration.GetConnectionString("Redis")!,
                        name: "redis-subscriptions",
                        tags: ["ready"]);
                ```

                ## Anti-patterns

                **Using in-memory subscriptions with multiple instances:**

                ```csharp
                // BAD: In-memory subscriptions only work within a single process.
                // With 3 instances behind a load balancer, each instance only sees
                // events published from its own process.
                builder.Services
                    .AddGraphQLServer()
                    .AddInMemorySubscriptions(); // Events are lost across instances!
                ```

                **Creating a new ConnectionMultiplexer per request:**

                ```csharp
                // BAD: ConnectionMultiplexer should be a singleton
                .AddRedisSubscriptions(sp =>
                    ConnectionMultiplexer.Connect(connectionString)); // Creates new connection each time!
                // Register it as a singleton instead
                ```

                **Not handling Redis connection failures:**

                ```csharp
                // BAD: No retry policy or fallback for Redis connectivity
                .AddRedisSubscriptions(sp =>
                    ConnectionMultiplexer.Connect(connectionString));
                // Configure retry policies and connection timeouts
                ```

                ## Key Points

                - Use `AddInMemorySubscriptions()` for single-instance deployments and development
                - Use `AddRedisSubscriptions()` for multi-instance deployments behind a load balancer
                - Register `ConnectionMultiplexer` as a singleton — do not create new connections per request
                - Always call `app.UseWebSockets()` before `app.MapGraphQL()` in the middleware pipeline
                - Switch between providers based on environment configuration
                - Add health checks for Redis to monitor subscription infrastructure

                ## Related Practices

                - [subscriptions-basic] — For subscription type definitions
                - [subscriptions-filtered] — For topic-based event filtering
                - [configuration-server-setup] — For server configuration
                """
            });
    }
}
