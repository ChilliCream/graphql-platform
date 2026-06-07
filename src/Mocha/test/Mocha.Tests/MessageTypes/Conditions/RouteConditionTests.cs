using System.Collections.Immutable;
using System.Reflection;
using Mocha.Tests.Middlewares.Receive;

namespace Mocha.Tests.MessageTypes.Conditions;

/// <summary>
/// Tests for the route condition family that decides whether an inbound route selects its consumer
/// for a received message.
/// </summary>
public class RouteConditionTests : ReceiveMiddlewareTestBase
{
    private static readonly ContextDataKey<string> s_correlationKey = new("test-correlation");
    private static readonly ContextDataKey<string> s_sagaIdKey = new("test-saga-id");

    [Fact]
    public void MessageTypeCondition_Should_Match_When_ContextTypeIsExactType()
    {
        // arrange
        var messageType = CreateMessageType();
        var condition = new MessageTypeCondition(messageType);
        var context = new StubReceiveContext { MessageType = messageType };

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.True(matches);
    }

    [Fact]
    public void MessageTypeCondition_Should_Match_When_ContextTypeEnclosesRouteType()
    {
        // arrange - route targets the base type, the context carries a derived type
        var baseType = CreateMessageType();
        var derivedType = CreateMessageType(enclosed: [baseType]);
        var condition = new MessageTypeCondition(baseType);
        var context = new StubReceiveContext { MessageType = derivedType };

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.True(matches);
    }

    [Fact]
    public void MessageTypeCondition_Should_NotMatch_When_ContextTypeIsUnrelated()
    {
        // arrange
        var routeType = CreateMessageType();
        var otherType = CreateMessageType();
        var condition = new MessageTypeCondition(routeType);
        var context = new StubReceiveContext { MessageType = otherType };

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.False(matches);
    }

    [Fact]
    public void MessageTypeCondition_Should_NotMatch_When_RouteTypeIsObjectAndContextIsConcrete()
    {
        // arrange - the structural reason OnAnyReply (OnReply<object>) cannot route by type:
        // object is excluded from every concrete message's enclosed types
        var objectType = CreateMessageType();
        var concreteType = CreateMessageType();
        var condition = new MessageTypeCondition(objectType);
        var context = new StubReceiveContext { MessageType = concreteType };

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.False(matches);
    }

    [Fact]
    public void MessageTypeCondition_Should_NotMatch_When_ContextTypeIsNull()
    {
        // arrange
        var condition = new MessageTypeCondition(CreateMessageType());
        var context = new StubReceiveContext { MessageType = null };

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.False(matches);
    }

    [Fact]
    public void MessageTypeCondition_Should_Match_When_OptionalAndContextTypeIsNull()
    {
        // arrange - an optional reply route still selects when the message has no resolved type
        var condition = new MessageTypeCondition(CreateMessageType(), optional: true);
        var context = new StubReceiveContext { MessageType = null };

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.True(matches);
    }

    [Fact]
    public void MessageTypeCondition_Should_NotMatch_When_StrictAndContextTypeIsNull()
    {
        // arrange - a strict route requires a resolved type to match
        var condition = new MessageTypeCondition(CreateMessageType(), optional: false);
        var context = new StubReceiveContext { MessageType = null };

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.False(matches);
    }

    [Fact]
    public void MessageTypeCondition_Should_NotMatch_When_OptionalAndWrongTypePresent()
    {
        // arrange - a present but unrelated type fails even for an optional route
        var routeType = CreateMessageType();
        var otherType = CreateMessageType();
        var condition = new MessageTypeCondition(routeType, optional: true);
        var context = new StubReceiveContext { MessageType = otherType };

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.False(matches);
    }

    [Fact]
    public void MessageTypeCondition_Should_Match_When_OptionalAndCorrectTypePresent()
    {
        // arrange
        var routeType = CreateMessageType();
        var condition = new MessageTypeCondition(routeType, optional: true);
        var context = new StubReceiveContext { MessageType = routeType };

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.True(matches);
    }

    [Fact]
    public void MessageTypeCondition_Should_Match_When_OptionalAndEnclosedTypePresent()
    {
        // arrange - the optional route targets the base type, the context carries a derived type
        var baseType = CreateMessageType();
        var derivedType = CreateMessageType(enclosed: [baseType]);
        var condition = new MessageTypeCondition(baseType, optional: true);
        var context = new StubReceiveContext { MessageType = derivedType };

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.True(matches);
    }

    [Fact]
    public void HeaderPresentCondition_Should_Match_When_HeaderPresent()
    {
        // arrange
        var condition = new HeaderPresentCondition<string>(s_correlationKey);
        var context = new StubReceiveContext();
        context.Headers.Set(s_correlationKey, "abc");

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.True(matches);
    }

    [Fact]
    public void HeaderPresentCondition_Should_NotMatch_When_HeaderMissing()
    {
        // arrange
        var condition = new HeaderPresentCondition<string>(s_correlationKey);
        var context = new StubReceiveContext();

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.False(matches);
    }

    [Fact]
    public void AndCondition_Should_Match_When_AllChildrenMatch()
    {
        // arrange
        var condition = AndCondition.Create(
            new HeaderPresentCondition<string>(s_sagaIdKey),
            new HeaderPresentCondition<string>(s_correlationKey));
        var context = new StubReceiveContext();
        context.Headers.Set(s_sagaIdKey, "saga");
        context.Headers.Set(s_correlationKey, "abc");

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.True(matches);
    }

    [Fact]
    public void AndCondition_Should_NotMatch_When_FirstTermFails()
    {
        // arrange - the correlation header is present but the saga-id header is missing
        var condition = AndCondition.Create(
            new HeaderPresentCondition<string>(s_sagaIdKey),
            new HeaderPresentCondition<string>(s_correlationKey));
        var context = new StubReceiveContext();
        context.Headers.Set(s_correlationKey, "abc");

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.False(matches);
    }

    [Fact]
    public void AndCondition_Should_NotMatch_When_SecondTermFails()
    {
        // arrange - the saga-id header is present but the correlation header is missing
        var condition = AndCondition.Create(
            new HeaderPresentCondition<string>(s_sagaIdKey),
            new HeaderPresentCondition<string>(s_correlationKey));
        var context = new StubReceiveContext();
        context.Headers.Set(s_sagaIdKey, "saga");

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.False(matches);
    }

    [Fact]
    public void NoMatchCondition_Should_NeverMatch()
    {
        // arrange
        var condition = NoMatchCondition.Instance;
        var context = new StubReceiveContext { MessageType = CreateMessageType() };
        context.Headers.SetMessageKind(MessageKind.Reply);

        // act
        var matches = condition.Matches(context);

        // assert
        Assert.False(matches);
    }

    private static MessageType CreateMessageType(ImmutableArray<MessageType>? enclosed = null)
    {
        var mt = new MessageType();
        SetPrivateProperty(mt, nameof(MessageType.Identity), "urn:message:Test");
        if (enclosed.HasValue)
        {
            SetPrivateProperty(mt, nameof(MessageType.EnclosedMessageTypes), enclosed.Value);
        }
        return mt;
    }

    private static void SetPrivateProperty<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property!.SetValue(target, value);
    }
}
