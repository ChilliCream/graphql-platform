using System;
using Xunit;

namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public class ExchangeNameFactoryTests
{
    class Topic
    {
        public int Id { get; set; }
    }

    [Fact]
    public void Create_StringTopic()
    {
        string exhcnageName = Sut().Create("test-topic-1");
        Assert.Equal("test-topic-1", exhcnageName);
    }

    [Fact]
    public void Create_ClassTopic()
    {
        string exhcnageName = Sut().Create(new Topic { Id = 50 });
        Assert.Equal("HotChocolate.Subscriptions.RabbitMQ.Serialization.ExchangeNameFactoryTests+Topic: {\"$type\":\"HotChocolate.Subscriptions.RabbitMQ.Serialization.ExchangeNameFactoryTests+Topic, HotChocolate.Subscriptions.RabbitMQ.Tests\",\"Id\":50}", exhcnageName);
    }

    [Fact]
    public void Create_NullTopic()
    {
        Assert.Throws<ArgumentNullException>(() => Sut().Create<string>(null!));
    }

    private IExchangeNameFactory Sut()
        => new ExchangeNameFactory(new JsonSerializer());
}
