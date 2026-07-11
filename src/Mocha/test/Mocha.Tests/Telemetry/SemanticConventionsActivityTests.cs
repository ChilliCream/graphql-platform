using System.Diagnostics;

namespace Mocha.Tests;

public class SemanticConventionsActivityTests
{
    [Fact]
    public void SemanticConventions_SetOperationName_Should_SetTag_When_ActivityProvided()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.SetOperationName("test-operation");

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);
        var tags = activity.TagObjects.ToList();
        Assert.Contains(
            tags,
            kvp => kvp.Key == "messaging.operation.name" && kvp.Value?.ToString() == "test-operation");

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetConsumerName_Should_SetTag_When_ValueProvided()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.SetConsumerName("consumer1");

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);
        var tags = activity.TagObjects.ToList();
        Assert.Contains(tags, kvp => kvp.Key == "messaging.handler.name" && kvp.Value?.ToString() == "consumer1");

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetConsumerName_Should_ReturnActivityUnchanged_When_ValueIsNull()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.SetConsumerName(null);

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetDestinationAddress_Should_SetTag_When_UriProvided()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();
        var uri = new Uri("amqp://localhost:5672");

        // act
        var result = activity.SetDestinationAddress(uri);

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);
        var tags = activity.TagObjects.ToList();
        Assert.Contains(tags, kvp => kvp.Key == "messaging.destination.address");

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetDestinationAddress_Should_ReturnActivityUnchanged_When_UriIsNull()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.SetDestinationAddress(null);

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetDestinationTemporary_Should_SetTag_When_BoolProvided()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.SetDestinationTemporary(true);

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);
        var tags = activity.TagObjects.ToList();
        Assert.Contains(tags, kvp => kvp.Key == "messaging.destination.temporary" && (bool)kvp.Value! == true);

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetInstanceId_Should_SetTag_When_GuidProvided()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();
        var id = Guid.NewGuid();

        // act
        var result = activity.SetInstanceId(id);

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);
        var tags = activity.TagObjects.ToList();
        Assert.Contains(tags, kvp => kvp.Key == "messaging.instance.id" && kvp.Value?.ToString() == id.ToString());

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetConversationId_Should_SetTag_When_ValueProvided()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.SetConversationId("conv-123");

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);
        var tags = activity.TagObjects.ToList();
        Assert.Contains(
            tags,
            kvp => kvp.Key == "messaging.message.conversation_id" && kvp.Value?.ToString() == "conv-123");

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetConversationId_Should_ReturnActivityUnchanged_When_ValueIsNull()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.SetConversationId(null);

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetMessageId_Should_SetTag_When_ValueProvided()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.SetMessageId("msg-456");

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);
        var tags = activity.TagObjects.ToList();
        Assert.Contains(tags, kvp => kvp.Key == "messaging.message.id" && kvp.Value?.ToString() == "msg-456");

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetBodySize_Should_SetTag_When_SizeProvided()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.SetBodySize(1024L);

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);
        var tags = activity.TagObjects.ToList();
        Assert.Contains(tags, kvp => kvp.Key == "messaging.message.body.size" && (long)kvp.Value! == 1024L);

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_EnrichMessageDefault_Should_SetMessagingSystem_When_Called()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.EnrichMessageDefault();

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);

        activity.Stop();
    }

    [Fact]
    public void SemanticConventions_SetMessagingSystem_Should_ReturnActivity_When_Called()
    {
        // arrange
        var activity = new Activity("test");
        activity.Start();

        // act
        var result = activity.SetMessagingSystem();

        // assert
        Assert.NotNull(result);
        Assert.Same(activity, result);

        activity.Stop();
    }
}
