using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace HotChocolate.Subscriptions.RabbitMQ;

public class ActiveConsumerTests
{
    [Fact]
    public void Consumer()
    {
        Mock<IModel> channel = new();

        AsyncEventingBasicConsumer consumer = new(channel.Object);
        ActiveConsumer sut = new(consumer, () => { });

        Assert.Equal(consumer, sut.Consumer);
    }

    [Fact]
    public void Listen_Unlisten()
    {
        Mock<IModel> channel = new();

        AsyncEventingBasicConsumer consumer = new(channel.Object);

        bool empty = false;
        ActiveConsumer sut = new(consumer, () => empty = true);

        Action unlisten1 = sut.Listen((sender, args) => Task.CompletedTask);
        Action unlisten2 = sut.Listen((sender, args) => Task.CompletedTask);

        Assert.False(empty);

        unlisten2();
        unlisten1();

        Assert.True(empty);
    }

    [Fact]
    public async Task Listen_Recieve()
    {
        Mock<IModel> channel = new();
        AsyncEventingBasicConsumer consumer = new(channel.Object);
        ActiveConsumer sut = new(consumer, () => {});

        List<string> recieved = new();
        Action unlisten = sut.Listen((sender, args) =>
        {
            string msg = Encoding.UTF8.GetString(args.Body.ToArray());
            recieved.Add(msg);
            return Task.CompletedTask;
        });

        await consumer.HandleBasicDeliver("test", 1, false, "test-exchange", "", null, Encoding.UTF8.GetBytes("hello world"));
        await consumer.HandleBasicDeliver("test", 1, false, "test-exchange", "", null, Encoding.UTF8.GetBytes("hello world again"));
        unlisten();
        await consumer.HandleBasicDeliver("test", 1, false, "test-exchange", "", null, Encoding.UTF8.GetBytes("hello world to noone"));

        Assert.Equal(new[] {"hello world", "hello world again"}, recieved);
    }

    [Fact]
    public async Task Listen_RecieveConcurently()
    {
        Mock<IModel> channel = new();
        AsyncEventingBasicConsumer consumer = new(channel.Object);
        ActiveConsumer sut = new(consumer, () => { });

        List<string> recieved1 = new();
        Action unlisten1 = sut.Listen((sender, args) =>
        {
            string msg = Encoding.UTF8.GetString(args.Body.ToArray());
            recieved1.Add(msg);
            return Task.CompletedTask;
        });
        List<string> recieved2 = new();
        sut.Listen((sender, args) =>
        {
            string msg = Encoding.UTF8.GetString(args.Body.ToArray());
            recieved2.Add(msg);
            return Task.CompletedTask;
        });

        await consumer.HandleBasicDeliver("test", 1, false, "test-exchange", "", null, Encoding.UTF8.GetBytes("hello world"));
        await consumer.HandleBasicDeliver("test", 1, false, "test-exchange", "", null, Encoding.UTF8.GetBytes("hello world again"));
        unlisten1();
        await consumer.HandleBasicDeliver("test", 1, false, "test-exchange", "", null, Encoding.UTF8.GetBytes("hello world for second"));

        Assert.Equal(new[] { "hello world", "hello world again" }, recieved1);
        Assert.Equal(new[] { "hello world", "hello world again", "hello world for second" }, recieved2);
    }
}
