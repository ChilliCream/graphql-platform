using CookieCrumble;

namespace Mocha.Tests.Descriptions;

public sealed class MochaUrnTests
{
    [Fact]
    public void MessageType_Should_UseIdentity()
    {
        // act
        var urn = MochaUrn.MessageType("urn:message:order-created");

        // assert
        urn.MatchInlineSnapshot("urn:message:order-created");
    }

    [Fact]
    public void Transport_Should_ScopeToServiceByName()
    {
        // act
        var urn = MochaUrn.Transport("orders", "memory", "default");

        // assert
        urn.MatchInlineSnapshot("urn:mocha:svc:orders:transport:memory:default");
    }

    [Fact]
    public void TopologyLink_Should_UseSourceAndTarget_When_NoAddress()
    {
        // act
        var urn = MochaUrn.TopologyLink(null, "bind", "queue:a", "queue:b");

        // assert
        urn.MatchInlineSnapshot("urn:mocha:link:bind:queue:a~queue:b");
    }

    [Fact]
    public void TopologyLink_Should_UseAddress_When_AddressProvided()
    {
        // act
        var urn = MochaUrn.TopologyLink("memory://orders/b/t/a/q/b", "bind", "queue:a", "queue:b");

        // assert
        urn.MatchInlineSnapshot("urn:mocha:link:memory://orders/b/t/a/q/b");
    }

    [Fact]
    public void TopologyEntity_Should_UseKindAndName_When_NoAddress()
    {
        // act
        var urn = MochaUrn.TopologyEntity(null, "queue", "orders");

        // assert
        urn.MatchInlineSnapshot("urn:mocha:topology:queue:orders");
    }

    [Fact]
    public void TopologyEntity_Should_UseAddress_When_AddressProvided()
    {
        // act
        var urn = MochaUrn.TopologyEntity("memory://orders/q/orders", "queue", "orders");

        // assert
        urn.MatchInlineSnapshot("urn:mocha:topology:memory://orders/q/orders");
    }

    [Fact]
    public void Consumer_Should_ScopeToService()
    {
        // act
        var urn = MochaUrn.Consumer("orders", "order-created");

        // assert
        urn.MatchInlineSnapshot("urn:mocha:svc:orders:consumer:order-created");
    }

    [Fact]
    public void DispatchEndpoint_Should_ScopeToTransportByName()
    {
        // act
        var urn = MochaUrn.DispatchEndpoint("orders", "memory", "default", "order-created");

        // assert
        urn.MatchInlineSnapshot("urn:mocha:svc:orders:transport:memory:default:dispatch-endpoint:order-created");
    }

    [Fact]
    public void ReceiveEndpoint_Should_ScopeToTransportByName()
    {
        // act
        var urn = MochaUrn.ReceiveEndpoint("orders", "memory", "default", "order-created");

        // assert
        urn.MatchInlineSnapshot("urn:mocha:svc:orders:transport:memory:default:receive-endpoint:order-created");
    }

    [Fact]
    public void EndpointUrns_Should_Differ_When_DispatchAndReceiveShareName()
    {
        // act
        var dispatchUrn = MochaUrn.DispatchEndpoint("orders", "memory", "default", "orders");
        var receiveUrn = MochaUrn.ReceiveEndpoint("orders", "memory", "default", "orders");

        // assert
        Assert.NotEqual(dispatchUrn, receiveUrn);
    }

    [Fact]
    public void EndpointUrns_Should_Differ_When_TransportsShareEndpointName()
    {
        // act
        var defaultUrn = MochaUrn.DispatchEndpoint("orders", "memory", "default", "orders");
        var secondaryUrn = MochaUrn.DispatchEndpoint("orders", "memory", "secondary", "orders");

        // assert
        Assert.NotEqual(defaultUrn, secondaryUrn);
    }

    [Fact]
    public void OutboundRoute_Should_UseEndpointNameAndHashMessageType()
    {
        // act
        var urn = MochaUrn.OutboundRoute(
            "orders",
            "publish",
            "urn:message:order-created",
            "q/order-created");

        // assert
        urn.MatchInlineSnapshot("urn:mocha:svc:orders:outbound-route:publish:q/order-created:43ee87a91ef59b12");
    }

    [Fact]
    public void InboundRoute_Should_BeStable_When_CompositeConditionChildrenReordered()
    {
        // arrange
        var a = new RouteConditionDescription("message-type", "urn:message:a", []);
        var b = new RouteConditionDescription("header", "tenant", []);
        var ab = new RouteConditionDescription("and", null, [a, b]);
        var ba = new RouteConditionDescription("and", null, [b, a]);

        // act & assert
        Assert.Equal(
            MochaUrn.InboundRoute("orders", "subscribe", "consumer", ab),
            MochaUrn.InboundRoute("orders", "subscribe", "consumer", ba));
    }

    [Fact]
    public void InboundRoute_Should_Differ_When_ConditionDetailsDiffer()
    {
        // arrange
        var a = new RouteConditionDescription("message-type", "urn:message:a", []);
        var b = new RouteConditionDescription("message-type", "urn:message:b", []);

        // act & assert
        Assert.NotEqual(
            MochaUrn.InboundRoute("orders", "subscribe", "consumer", a),
            MochaUrn.InboundRoute("orders", "subscribe", "consumer", b));
    }
}
