using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions.RabbitMQ.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace HotChocolate.Subscriptions.RabbitMQ;

public class RabbitMQSubscriptionsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRabbitMQSubscriptions_1_Services_Is_Null()
    {
        static void Fail()
            => default(IServiceCollection)!.AddRabbitMQSubscriptions(
                sp => sp.GetRequiredService<IConnection>());

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddRabbitMQSubscriptions_2_Services_Is_Null()
    {
        static void Fail() =>
            default(IRequestExecutorBuilder)!.AddRabbitMQSubscriptions(
                sp => sp.GetRequiredService<IModel>());

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddRabbitMQSubscriptions_2_Connection_Is_Null()
    {
        var builder = new Mock<IRequestExecutorBuilder>();
        void Fail() => builder.Object.AddRabbitMQSubscriptions(sp => sp.GetRequiredService<IConnection>(), default);
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddRabbitMQSubscriptions_3_Services_Is_Null()
    {
        void Fail() => default(IRequestExecutorBuilder)!.AddRabbitMQSubscriptions(sp => sp.GetRequiredService<IConnection>(), default);
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddRabbitMQSubscriptions_Resolve()
    {
        Mock<IModel> channel = new();
        Mock<IConnection> connection = new();
        connection.Setup(m => m.CreateModel()).Returns(channel.Object);

        IServiceProvider provider = new ServiceCollection()
            .AddRabbitMQSubscriptions(sp => connection.Object, opts =>
            {
                opts.InstanceName = "Test instance";
            }).BuildServiceProvider();

        provider.GetRequiredService<ITopicEventSender>();

        connection.Verify(m => m.CreateModel(), Times.Once);
    }
}
