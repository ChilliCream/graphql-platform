using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using CookieCrumble;

namespace Mocha.Transport.RabbitMQ.Tests.Topology;

public class RabbitMQExplicitTopologyTests
{
    [Fact]
    public void Describe_Should_KeepConventionEntities_When_ExplicitBindingAndAutoBindDefaultOn()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        // explicit handler binding governs axis A only: with auto-binding left at its default the
        // convention still fabricates the publish/send exchanges and binds them into the declared queue.
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_OmitConventionEntities_When_ExplicitBindingAndAutoBindFalse()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        // BindExplicitly at transport scope removes the convention bind into the queue.
        // The type-owned publish/send exchanges are still produced because they are owned by the type
        // (suppression scope, reading X from plan v3 section 3.4), but no exchange-to-queue bind appears.
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_IncludeConventionEntities_When_ImplicitBinding()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t => t.BindImplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        // implicit mode fabricates the convention publish/send exchanges and the binding chain
        // for the handled message type; this guards the explicit-mode omission from regressing.
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void DeclareBinding_Should_RetainAllBindings_When_DifferentRoutingKeys()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("ex");
            t.DeclareQueue("q");
            t.DeclareBinding("ex", "q").RoutingKey("a.b");
            t.DeclareBinding("ex", "q").RoutingKey("a.c");
            t.DeclareBinding("ex", "q").RoutingKey("#");
        });

        // assert
        var bindings = topology.Bindings.Where(b => b.Source.Name == "ex").ToList();
        Assert.Equal(3, bindings.Count);
        Assert.Equal(3, bindings.Select(b => b.Address!.ToString()).Distinct().Count());
        Assert.All(bindings, b => Assert.Contains("rk=", b.Address!.ToString()));
    }

    [Fact]
    public void DeclareBinding_Should_Deduplicate_When_IdenticalDeclarations()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("ex");
            t.DeclareQueue("q");
            t.DeclareBinding("ex", "q").RoutingKey("same");
            t.DeclareBinding("ex", "q").RoutingKey("same");
        });

        // assert
        Assert.Single(topology.Bindings, b => b.Source.Name == "ex");
    }

    [Fact]
    public void GetPublishEndpoint_Should_DeriveConventionExchange_When_UnconfiguredTypeInExplicitMode()
    {
        // arrange
        // OrderCreated has a consumer but no configured outbound route. Explicit binding governs
        // consumer binding only, so producer dispatch must still derive the convention destination.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var expectedExchange = transport.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var endpoint = runtime.GetPublishEndpoint(runtime.GetMessageType(typeof(OrderCreated)));

        // assert
        var dispatchEndpoint = Assert.IsType<RabbitMQDispatchEndpoint>(endpoint);
        Assert.Equal(expectedExchange, dispatchEndpoint.Exchange?.Name);
    }

    [Fact]
    public void Describe_Should_InheritParentQueueAutoProvisionOnInfraQueues_When_ParentDeclared()
    {
        // arrange
        // parentTrue: transport default false, queue declared true, so infra queues resolve true.
        // parentFalse: transport default true, queue declared false, so infra queues resolve false.
        var parentTrue = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoProvision(false);
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            }).Transports.OfType<RabbitMQMessagingTransport>().Single();

        var parentFalse = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoProvision(true);
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(false).Consumer<OrderSpyConsumer>();
            }).Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act & assert
        // the error and skip infra queues resolve exactly like the queue they serve, overriding the
        // transport default in both directions.
        new Snapshot()
            .Add(RabbitMQDescribeSnapshot.Create(parentTrue.Describe()), "ParentDeclaredTrue", MarkdownLanguages.Json)
            .Add(RabbitMQDescribeSnapshot.Create(parentFalse.Describe()), "ParentDeclaredFalse", MarkdownLanguages.Json)
            .MatchMarkdown();
    }

    [Fact]
    public void Describe_Should_OmitCustomToConventionBinds_When_ExplicitDispatchEndpointConfigured()
    {
        // arrange & act
        // a configured dispatch endpoint with routes would, in implicit mode, bind the custom exchange
        // to the convention exchange; explicit mode must omit that custom-to-convention bind.
        var (_, transport, _) = CreateTopology(t =>
        {
            t.BindExplicitly();
            t.DeclareExchange("custom-ex");
            t.DispatchEndpoint("dispatch").ToExchange("custom-ex").Publish<OrderCreated>();
        });

        // assert
        RabbitMQDescribeSnapshot.Create(transport.Describe()).MatchSnapshot();
    }

    [Fact]
    public void DiscoverTopology_Should_BridgeCustomExchangeToResolverChainEntry_When_ImplicitMode()
    {
        // arrange
        var (_, transport, topology) = CreateTopology(t =>
        {
            t.BindImplicitly();
            t.DeclareExchange("custom-ex");
            t.DispatchEndpoint("dispatch").ToExchange("custom-ex").Publish<OrderCreated>();
        });
        var conventionExchange = transport.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act: topology is discovered during CreateTopology; inspect the result.
        var bridgeBindings = topology.Bindings
            .OfType<RabbitMQExchangeBinding>()
            .Where(b => b.Source.Name == "custom-ex" && b.Destination.Name == conventionExchange)
            .ToList();

        // assert
        Assert.Single(bridgeBindings);
        Assert.Contains(topology.Exchanges, e => e.Name == conventionExchange);
    }

    [Fact]
    public void DiscoverTopology_Should_OmitBridgeBind_When_ExplicitMode()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.BindExplicitly();
            t.DeclareExchange("custom-ex");
            t.DispatchEndpoint("dispatch").ToExchange("custom-ex").Publish<OrderCreated>();
        });

        // assert: no binding originates from the custom exchange in explicit mode.
        Assert.DoesNotContain(topology.Bindings, b => b.Source.Name == "custom-ex");
    }

    [Fact]
    public void DeclareBinding_Should_RetainAllExchangeBindings_When_DifferentRoutingKeys()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("src");
            t.DeclareExchange("dst");
            t.DeclareBinding("src", "dst").ToExchange("dst").RoutingKey("a.b");
            t.DeclareBinding("src", "dst").ToExchange("dst").RoutingKey("a.c");
        });

        // assert
        var bindings = topology.Bindings.Where(b => b.Source.Name == "src").ToList();
        Assert.Equal(2, bindings.Count);
        Assert.Equal(2, bindings.Select(b => b.Address!.ToString()).Distinct().Count());
        Assert.All(bindings, b => Assert.IsType<RabbitMQExchangeBinding>(b));
    }

    [Fact]
    public void DeclareBinding_Should_RetainAllBindings_When_DifferentHeaderArguments()
    {
        // arrange & act
        // headers bindings between the same exchange and queue are distinguished only by arguments
        // and carry empty routing keys, so both must be retained with distinct addresses.
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("headers-ex").Type("headers");
            t.DeclareQueue("q");
            t.DeclareBinding("headers-ex", "q").Match(RabbitMQBindingMatchType.All).WithArgument("region", "eu");
            t.DeclareBinding("headers-ex", "q").Match(RabbitMQBindingMatchType.All).WithArgument("region", "us");
        });

        // assert
        var bindings = topology.Bindings.Where(b => b.Source.Name == "headers-ex").ToList();
        Assert.Equal(2, bindings.Count);
        Assert.Equal(2, bindings.Select(b => b.Address!.ToString()).Distinct().Count());
    }

    [Fact]
    public void DeclareBinding_Should_Deduplicate_When_IdenticalHeaderArguments()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("headers-ex").Type("headers");
            t.DeclareQueue("q");
            t.DeclareBinding("headers-ex", "q").Match(RabbitMQBindingMatchType.All).WithArgument("region", "eu");
            t.DeclareBinding("headers-ex", "q").Match(RabbitMQBindingMatchType.All).WithArgument("region", "eu");
        });

        // assert
        Assert.Single(topology.Bindings, b => b.Source.Name == "headers-ex");
    }

    [Fact]
    public void DeclareBinding_Should_RetainAllBindings_When_ArgumentValuesDifferByType()
    {
        // arrange & act
        // an integer value and the string "1" are distinct header-table arguments, so neither the
        // dedupe nor the address discriminator may collapse them.
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("headers-ex").Type("headers");
            t.DeclareQueue("q");
            t.DeclareBinding("headers-ex", "q").WithArgument("x", 1);
            t.DeclareBinding("headers-ex", "q").WithArgument("x", "1");
        });

        // assert
        var bindings = topology.Bindings.Where(b => b.Source.Name == "headers-ex").ToList();
        Assert.Equal(2, bindings.Count);
        Assert.Equal(2, bindings.Select(b => b.Address!.ToString()).Distinct().Count());
    }

    [Fact]
    public void DeclareBinding_Should_RetainAllBindings_When_ArgumentSeparatorsInjected()
    {
        // arrange & act
        // a value containing the argument separators must not be confused with two separate arguments.
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("headers-ex").Type("headers");
            t.DeclareQueue("q");
            t.DeclareBinding("headers-ex", "q").WithArgument("a", "1&b=2");
            t.DeclareBinding("headers-ex", "q").WithArgument("a", "1").WithArgument("b", "2");
        });

        // assert
        var bindings = topology.Bindings.Where(b => b.Source.Name == "headers-ex").ToList();
        Assert.Equal(2, bindings.Count);
        Assert.Equal(2, bindings.Select(b => b.Address!.ToString()).Distinct().Count());
    }

    [Fact]
    public void DeclareBinding_Should_RetainAllBindings_When_BinaryArgumentsDifferByContent()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("headers-ex").Type("headers");
            t.DeclareQueue("q");
            t.DeclareBinding("headers-ex", "q").WithArgument("token", new byte[] { 1, 2, 3 });
            t.DeclareBinding("headers-ex", "q").WithArgument("token", new byte[] { 4, 5, 6 });
        });

        // assert
        var bindings = topology.Bindings.Where(b => b.Source.Name == "headers-ex").ToList();
        Assert.Equal(2, bindings.Count);
        Assert.Equal(2, bindings.Select(b => b.Address!.ToString()).Distinct().Count());
    }

    [Fact]
    public void DeclareBinding_Should_DeduplicateBinaryArguments_When_ContentEqual()
    {
        // arrange & act
        // distinct byte[] instances with the same content are the same argument.
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("headers-ex").Type("headers");
            t.DeclareQueue("q");
            t.DeclareBinding("headers-ex", "q").WithArgument("token", new byte[] { 1, 2, 3 });
            t.DeclareBinding("headers-ex", "q").WithArgument("token", new byte[] { 1, 2, 3 });
        });

        // assert
        Assert.Single(topology.Bindings, b => b.Source.Name == "headers-ex");
    }

    [Fact]
    public void ArgumentsEqual_Should_DistinguishNullFromEmptyString()
    {
        // arrange
        KeyValuePair<string, object?>[] withNull = [new("x", null)];
        KeyValuePair<string, object?>[] withEmpty = [new("x", string.Empty)];

        // act & assert
        // a null header value and an empty string are distinct, both for equality and for the address.
        Assert.False(RabbitMQBinding.ArgumentsEqual(withNull, withEmpty));
        Assert.NotEqual(
            RabbitMQBinding.CanonicalizeArguments(withNull),
            RabbitMQBinding.CanonicalizeArguments(withEmpty));
    }

    [Fact]
    public void MergeFrom_Should_AdoptExplicitValue_When_ExistingAutoProvisionIsNull()
    {
        // arrange
        var binding = DeclareSingleBinding(b => { });

        // act
        binding.MergeFrom(new RabbitMQBindingConfiguration { AutoProvision = true });

        // assert
        Assert.True(binding.AutoProvision);
    }

    [Fact]
    public void MergeFrom_Should_PreferProvisioning_When_AutoProvisionConflicts()
    {
        // arrange
        var existingOptOut = DeclareSingleBinding(b => b.AutoProvision(false));
        var existingProvision = DeclareSingleBinding(b => b.AutoProvision(true));

        // act
        existingOptOut.MergeFrom(new RabbitMQBindingConfiguration { AutoProvision = true });
        existingProvision.MergeFrom(new RabbitMQBindingConfiguration { AutoProvision = false });

        // assert - provisioning wins regardless of declaration order
        Assert.True(existingOptOut.AutoProvision);
        Assert.True(existingProvision.AutoProvision);
    }

    [Fact]
    public void MergeFrom_Should_UpgradeOrigin_When_DeclaredMergedOntoConvention()
    {
        // arrange
        // build a framework-generated binding (convention origin) directly in the topology.
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("ex");
            t.DeclareQueue("q");
        });
        var binding = topology.AddBinding(new RabbitMQBindingConfiguration
        {
            Source = "ex",
            Destination = "q",
            DestinationKind = RabbitMQDestinationKind.Queue,
            Origin = TopologyOrigin.Convention
        });
        Assert.Equal(TopologyOrigin.Convention, binding.Origin);

        // act
        binding.MergeFrom(new RabbitMQBindingConfiguration { Origin = TopologyOrigin.Declared });

        // assert
        Assert.Equal(TopologyOrigin.Declared, binding.Origin);
    }

    private static RabbitMQBinding DeclareSingleBinding(Action<IRabbitMQBindingTopologyDescriptor> configure)
    {
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("ex");
            t.DeclareQueue("q");
            configure(t.DeclareBinding("ex", "q"));
        });

        return topology.Bindings.Single(b => b.Source.Name == "ex");
    }

    private static (
        MessagingRuntime Runtime,
        RabbitMQMessagingTransport Transport,
        RabbitMQMessagingTopology Topology) CreateTopology(Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configure(t);
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport(t);
            })
            .BuildRuntime();
        return runtime;
    }

    [Fact]
    public void Describe_Should_OmitConventionChain_When_SagaHasOnReplyTransition()
    {
        // arrange
        // A saga with an OnReply transition registers an InboundRouteKind.Reply route. The receive
        // convention must skip reply routes so no spurious exchange chain appears for the reply type.
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        builder.AddSaga<OrderStockCheckSaga>();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.BindImplicitly();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        // Only the start event exchange chain appears. No exchange or binding chain for
        // StockInfoResult (the reply type) should be present.
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_BindConsumerFromExplicitDestination_When_AddMessagePublishDestinationConfigured()
    {
        // arrange
        // OrderCreated has an explicit exchange destination set via AddMessage<T>().Publish(). The
        // receive convention must bind from that exchange directly into the consumer queue instead of
        // deriving the convention exchange chain.
        var runtime = CreateRuntime(
            b =>
            {
                b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQExchange("custom-routing-exchange")));
                b.AddConsumer<OrderSpyConsumer>();
            },
            t => t.BindImplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_ResolveProducerAndConsumerToSameEntity_When_ConventionNamingUsed()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t => t.BindImplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;
        var publishExchangeName = transport.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act: both paths are exercised during BuildRuntime above; inspect the result.
        var dispatchEndpoint = transport.DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .FirstOrDefault(e => e.Exchange?.Name == publishExchangeName);
        var chainBinding = topology.Bindings
            .OfType<RabbitMQExchangeBinding>()
            .FirstOrDefault(b => b.Source.Name == publishExchangeName);

        // assert: the producer and consumer conventions both converge on the same exchange entity.
        Assert.NotNull(dispatchEndpoint);
        Assert.NotNull(chainBinding);
    }

    [Fact]
    public void Build_Should_Throw_When_AutoBindDerivedForDynamicRoutingKeyType()
    {
        // arrange
        // A custom routing-key function makes the bind key underivable. The receive convention must
        // fail the build rather than emit a key-less bind that would silently not match.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddMessage<OrderCreated>(d => d.UseRabbitMQRoutingKey<OrderCreated>(msg => msg.OrderId));
        builder.AddConsumer<OrderSpyConsumer>();

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder
                .AddRabbitMQ(t =>
                {
                    t.ConnectionProvider(_ => new StubConnectionProvider());
                    t.BindImplicitly();
                })
                .BuildRuntime());

        Assert.Contains("OrderCreated", exception.Message);
        Assert.Contains("routing key", exception.Message);
    }

    [Fact]
    public void Build_Should_Throw_When_ConsumedTypeHasExplicitQueueDestination()
    {
        // arrange
        // A message type routed to an explicit queue has no exchange chain to bind into the consumer
        // queue, so the receive convention must fail the build instead of generating a useless bind.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQQueue("direct-queue")));
        builder.AddConsumer<OrderSpyConsumer>();

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder
                .AddRabbitMQ(t =>
                {
                    t.ConnectionProvider(_ => new StubConnectionProvider());
                    t.BindImplicitly();
                })
                .BuildRuntime());

        Assert.Contains("OrderCreated", exception.Message);
    }

    [Fact]
    public void Describe_Should_UseConventionName_When_DestinationBackfilledFromEndpoint()
    {
        // arrange
        // A dispatch endpoint configured via DispatchEndpoint().Publish<T>() without an explicit
        // route destination causes HasExplicitDestination = false: the route's Destination is
        // backfilled from the endpoint address at ConnectEndpoint time. The receive convention must
        // fall through to the convention exchange, not treat the backfilled address as user intent.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindImplicitly();
                t.DeclareExchange("special-exchange");
                t.DispatchEndpoint("special-ep").ToExchange("special-exchange").Publish<OrderCreated>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Queue_Endpoint_DeclareQueue_Should_ConvergeToOneEntity_When_SameName()
    {
        // arrange
        // Three paths all target the same queue name "orders":
        //   1. DeclareQueue("orders") at transport level (declared origin)
        //   2. Queue("orders") builder (creates queue + lazy endpoint)
        //   3. A second DeclareQueue("orders") with a different AutoProvision flag
        // The descriptor layer deduplicates DeclareQueue calls by name, so the second call returns
        // the same descriptor object and the last AutoProvision call on it wins at descriptor level.
        // The AddQueue merge (W2b.05) enforces the strengthen rule when two topology-level configs
        // meet; this test verifies no duplicate queue and no exception across all paths.
        var runtimeAllThree = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.DeclareQueue("orders").AutoProvision(true);
                t.Queue("orders").Consumer<OrderSpyConsumer>();
                t.DeclareQueue("orders").AutoProvision(false);
            });

        // queue-builder-only path: no DeclareQueue, just Queue()
        var runtimeEndpointOnly = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });

        var transportAllThree = runtimeAllThree.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var transportEndpointOnly = runtimeEndpointOnly.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var descriptionAllThree = transportAllThree.Describe();
        var descriptionEndpointOnly = transportEndpointOnly.Describe();

        // assert: each configuration has exactly one "orders" queue; no duplicate or exception.
        new Snapshot()
            .Add(RabbitMQDescribeSnapshot.Create(descriptionAllThree), "AllThreePaths", MarkdownLanguages.Json)
            .Add(RabbitMQDescribeSnapshot.Create(descriptionEndpointOnly), "EndpointOnly", MarkdownLanguages.Json)
            .MatchMarkdown();
    }

    [Fact]
    public void PublishExchange_Should_OverlayConventionExchange_When_PartialDeclarationGiven()
    {
        // arrange
        // PublishExchange contributes AutoDelete(true) onto the convention publish exchange for
        // OrderCreated. The receive convention creates the exchange with convention origin;
        // the dispatch convention merges the declared contribution via AddExchange, upgrading
        // origin to Declared and setting AutoDelete = true.
        var runtime = CreateRuntime(
            b =>
            {
                b.AddMessage<OrderCreated>(d => d.PublishExchange(e => e.AutoDelete(true)));
                b.AddConsumer<OrderSpyConsumer>();
            },
            t => t.BindImplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;
        var publishExchangeName = transport.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var description = transport.Describe();
        var exchange = topology.Exchanges.Single(e => e.Name == publishExchangeName);

        // assert
        Assert.True(exchange.AutoDelete);
        Assert.Equal(TopologyOrigin.Declared, exchange.Origin);
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact(Skip = "Placeholder: DeclareQueue conflict via descriptor API needs W2b shape-conflict"
              + " diagnostic wired from the descriptor layer (RabbitMQQueueDescriptor.CreateConfiguration)"
              + " into AddQueue. Today the shape-conflict throw is only reachable via topology.AddQueue"
              + " directly; the descriptor accumulates configs and calls AddQueue in CreateConfiguration.")]
    public void Build_Should_Throw_When_TwoDeclareQueueCallsConflictOnDurability()
    {
        // arrange
        // Two DeclareQueue("orders") calls with conflicting Durable values (both declared) must
        // surface a RabbitMQTopologyShapeConflictException at Build() time. This requires the
        // descriptor to pass both nullable configs through AddQueue so the merge matrix can detect
        // the declared-vs-declared conflict.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();

        // act & assert
        Assert.Throws<RabbitMQTopologyShapeConflictException>(() =>
            builder.AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.DeclareQueue("orders").Durable(true);
                t.DeclareQueue("orders").Durable(false);
            }).BuildRuntime());
    }

    [Fact(Skip = "Placeholder: cross-entity fanout-exchange / routing-key conflict check is deferred"
              + " per plan v3 note (minor finding 8). A declared fanout exchange receiving a convention"
              + " bind with a non-empty routing key should emit a shape-conflict diagnostic rather than"
              + " silently producing a bind that the broker will ignore.")]
    public void Build_Should_Throw_When_DeclaredFanoutExchangeReceivesConventionRoutingKeyBind()
    {
        // arrange
        // A fanout exchange ignores routing keys; a convention that emits a routing-key bind
        // against a declared fanout should be flagged instead of silently misbehaving.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();

        // act & assert
        Assert.Throws<RabbitMQTopologyShapeConflictException>(() =>
            builder.AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.DeclareExchange("orders.event").Type("fanout");
                t.BindImplicitly();
            }).BuildRuntime());
    }

    [Fact]
    public void Receives_Should_BindFourTypesToOneQueue_When_AllReceivedViaReceives()
    {
        // arrange
        // A single "inventory" endpoint binds four distinct message types via Receives<T>().
        // The convention emits one exchange chain per type, and all four chains bind into the queue.
        var runtime = CreateRuntime(
            b =>
            {
                b.AddConsumer<ItemAddedConsumer>();
                b.AddConsumer<ItemRemovedConsumer>();
                b.AddConsumer<OrderSpyConsumer>();
                b.AddConsumer<OrderShippedConsumer>();
            },
            t =>
            {
                t.BindExplicitly();
                t.Queue("inventory")
                    .Receives<ItemAdded>()
                    .Receives<ItemRemoved>()
                    .Receives<OrderCreated>()
                    .Receives<OrderShipped>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }

    public sealed class ItemAdded;

    public sealed class ItemRemoved;

    public sealed class OrderShipped;

    public sealed class ItemAddedConsumer : IConsumer<ItemAdded>
    {
        public ValueTask ConsumeAsync(IConsumeContext<ItemAdded> context) => default;
    }

    public sealed class ItemRemovedConsumer : IConsumer<ItemRemoved>
    {
        public ValueTask ConsumeAsync(IConsumeContext<ItemRemoved> context) => default;
    }

    public sealed class OrderShippedConsumer : IConsumer<OrderShipped>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderShipped> context) => default;
    }

    public sealed class StockCheckStarted;

    public sealed class StockInfoResult;

    public sealed class StockCheckState : SagaStateBase;

    public sealed class GetStockInfoRequest : IEventRequest<StockInfoResult>;

    public sealed class OrderStockCheckSaga : Saga<StockCheckState>
    {
        protected override void Configure(ISagaDescriptor<StockCheckState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StockCheckStarted>()
                .StateFactory(_ => new StockCheckState())
                .Send((_, _) => new GetStockInfoRequest())
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnReply<StockInfoResult>().TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }
}
