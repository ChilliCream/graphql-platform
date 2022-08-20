using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Subscriptions.RabbitMQ.Configuration;
using HotChocolate.Subscriptions.RabbitMQ.Serialization;
using Moq;
using Moq.Language;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace HotChocolate.Subscriptions.RabbitMQ;

public class RabbitMQPubSubTests
{
    class Car
    {
        public int Id { get; set; }
        public string? Brand { get; set; }
    }

    [Fact]
    public async Task SingleTopic_Complete()
    {
        RabbitMQPubSub sut = Sut();

        ISourceStream<Car> stream = await sut.SubscribeAsync<string, Car>("new-car");
        ValueTask<Car[]> task = stream.ReadEventsAsync().ToArrayAsync();

        await sut.SendAsync("new-car", new Car { Id = 1, Brand = "Škoda" });
        await sut.SendAsync("new-car", new Car { Id = 2, Brand = "Volkswagen" });
        await sut.SendAsync("new-car", new Car { Id = 3, Brand = "Mercedes-Benz" });
        await sut.CompleteAsync("new-car");
        await sut.SendAsync("new-car", new Car { Id = 4, Brand = "Audi" });

        Car[] cars = await task;
        Assert.Equal(new[] {1,2,3}, cars.Select(c => c.Id));
        Assert.Equal(new[] { "Škoda", "Volkswagen", "Mercedes-Benz" }, cars.Select(c => c.Brand));
    }

    [Fact]
    public async Task SingleTopic_Dispose()
    {
        RabbitMQPubSub sut = Sut();

        ISourceStream<Car> stream = await sut.SubscribeAsync<string, Car>("new-car");
        ValueTask<Car[]> task = stream.ReadEventsAsync().ToArrayAsync();

        await sut.SendAsync("new-car", new Car { Id = 1, Brand = "Škoda" });
        await sut.SendAsync("new-car", new Car { Id = 2, Brand = "Volkswagen" });
        await sut.SendAsync("new-car", new Car { Id = 3, Brand = "Mercedes-Benz" });
        await stream.DisposeAsync();
        await sut.SendAsync("new-car", new Car { Id = 4, Brand = "Audi" });

        Car[] cars = await task;
        Assert.Equal(new[] { 1, 2, 3 }, cars.Select(c => c.Id));
        Assert.Equal(new[] { "Škoda", "Volkswagen", "Mercedes-Benz" }, cars.Select(c => c.Brand));
    }

    [Fact]
    public async Task SingleTopic_MultipleStreams()
    {
        RabbitMQPubSub sut = Sut();

        ISourceStream<Car> stream1 = await sut.SubscribeAsync<string, Car>("new-car");
        ISourceStream<Car> stream2 = await sut.SubscribeAsync<string, Car>("new-car");
        ISourceStream<Car> stream3 = await sut.SubscribeAsync<string, Car>("new-car");

        ValueTask<Car[]> task1 = stream1.ReadEventsAsync().ToArrayAsync();
        ValueTask<Car[]> task2 = stream2.ReadEventsAsync().ToArrayAsync();
        ValueTask<Car[]> task3 = stream3.ReadEventsAsync().ToArrayAsync();

        await sut.SendAsync("new-car", new Car { Id = 1, Brand = "Škoda" });
        await sut.SendAsync("new-car", new Car { Id = 2, Brand = "Volkswagen" });

        // Stream 1 never gets MB and Audi
        await stream1.DisposeAsync();

        await sut.SendAsync("new-car", new Car { Id = 3, Brand = "Mercedes-Benz" });

        // Niether of stream get Audi
        await sut.CompleteAsync("new-car");

        await sut.SendAsync("new-car", new Car { Id = 4, Brand = "Audi" });

        // As stream 4 did not texist hen Audi was transmitted, neither stream 4 gets Audi
        ISourceStream<Car> stream4 = await sut.SubscribeAsync<string, Car>("new-car");

        // However stream 4 get's Opel as it was already existing when Opel was published even though noone was reading yet
        await sut.SendAsync("new-car", new Car { Id = 5, Brand = "Opel" });

        ValueTask<Car[]> task4 = stream4.ReadEventsAsync().ToArrayAsync();
        await stream4.DisposeAsync();

        Car[] cars1 = await task1;
        Assert.Equal(new[] { 1, 2, }, cars1.Select(c => c.Id));
        Assert.Equal(new[] { "Škoda", "Volkswagen" }, cars1.Select(c => c.Brand));

        Car[] cars2 = await task2;
        Assert.Equal(new[] { 1, 2, 3 }, cars2.Select(c => c.Id));
        Assert.Equal(new[] { "Škoda", "Volkswagen", "Mercedes-Benz" }, cars2.Select(c => c.Brand));

        Car[] cars3 = await task3;
        Assert.Equal(cars2.Select(c => c.Id), cars3.Select(c => c.Id));
        Assert.Equal(cars2.Select(c => c.Brand), cars3.Select(c => c.Brand));

        Car[] cars4 = await task4;
        Assert.Equal(new[] { 5 }, cars4.Select(c => c.Id));
        Assert.Equal(new[] { "Opel" }, cars4.Select(c => c.Brand));
    }

    [Fact]
    public async Task MultipleTopic_ListenForSomethingElse()
    {
        RabbitMQPubSub sut = Sut();

        ISourceStream<Car> stream = await sut.SubscribeAsync<string, Car>("car-sold");
        ValueTask<Car[]> task = stream.ReadEventsAsync().ToArrayAsync();

        await sut.SendAsync("new-car", new Car { Id = 1, Brand = "Škoda" });
        await sut.CompleteAsync("car-sold");
        await sut.CompleteAsync("new-car");

        Car[] cars = await task;
        Assert.Empty(cars);
    }

    [Fact]
    public async Task MultipleTopic()
    {
        RabbitMQPubSub sut = Sut();

        ISourceStream<Car> stream1 = await sut.SubscribeAsync<string, Car>("car-sold");
        ISourceStream<Car> stream2 = await sut.SubscribeAsync<string, Car>("new-car");
        ValueTask<Car[]> task1 = stream1.ReadEventsAsync().ToArrayAsync();
        ValueTask<Car[]> task2 = stream2.ReadEventsAsync().ToArrayAsync();

        await sut.SendAsync("new-car", new Car { Id = 1, Brand = "Škoda" });
        await sut.SendAsync("new-car", new Car { Id = 2, Brand = "Dacia" });
        await sut.SendAsync("car-sold", new Car { Id = 1, Brand = "Škoda" });
        await sut.SendAsync("new-car", new Car { Id = 3, Brand = "Lancia" });

        await sut.CompleteAsync("new-car");
        await sut.CompleteAsync("car-sold");

        Car[] soldCars = await task1;
        Assert.Equal(new[] { 1 }, soldCars.Select(c => c.Id));
        Assert.Equal(new[] { "Škoda" }, soldCars.Select(c => c.Brand));
        Car[] newCars = await task2;
        Assert.Equal(new[] { 1, 2, 3 }, newCars.Select(c => c.Id));
        Assert.Equal(new[] { "Škoda", "Dacia", "Lancia" }, newCars.Select(c => c.Brand));
    }

    public RabbitMQPubSub Sut()
    {
        HashSet<string> declaredExchanges = new();
        HashSet<string> declaredQueues = new();

        Dictionary<string, IBasicConsumer> consumers = new();
        Dictionary<string, string> queueToConsumer = new();
        Dictionary<string, List<string>> exchangesToQueue = new();

        Mock<IModel> channel = new();

        channel
            .Setup(m => m.ExchangeDeclare(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object>>()))
            .Callback<string, string, bool, bool, IDictionary<string, object>>(
                (exchange, type, durable, autoDelete, arguments) =>
                {
                    declaredExchanges.Add(exchange);
                    exchangesToQueue.Add(exchange, new List<string>());
                });

        channel
            .Setup(m => m.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object>>()))
            .Callback<string, bool, bool, bool, IDictionary<string, object>>(
                (queue, exclusive, durable, autoDelete, arguments) =>
                {
                    declaredQueues.Add(queue);
                });

        channel
            .Setup(m => m.QueueBind(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
            .Callback<string, string, string, IDictionary<string, object>>(
                (queue, exchange, routing, arguments) =>
                {
                    if (!declaredExchanges.Contains(exchange))
                        throw new Exception($"Exchange {exchange} was not declared!");
                    if (!declaredQueues.Contains(queue))
                        throw new Exception($"Queue {queue} was not declared!");

                    exchangesToQueue[exchange].Add(queue);
                });

        channel
            .Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<IBasicConsumer>()))
            .Returns<string, bool, string, bool, bool, IDictionary<string, object>, IBasicConsumer>(
                (queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer) =>
                {
                    if (!declaredQueues.Contains(queue))
                        throw new Exception($"Queue {queue} was not declared!");

                    string name = Guid.NewGuid().ToString();
                    consumers.Add(name, consumer);
                    queueToConsumer.Add(queue, name);
                    return name;
                });

        channel
            .Setup(m => m.BasicCancel(It.IsAny<string>()))
            .Callback<string>(consumerTag =>
            {
                string queue = queueToConsumer.Single(p => p.Value == consumerTag).Key;
                queueToConsumer.Remove(queue);
                consumers.Remove(consumerTag);
            });

        channel
            .Setup(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()))
            .Callback<string, string, bool, IBasicProperties, ReadOnlyMemory<byte>>(
                (exchange, routing, mandatory, basicProperties, body) =>
                {
                    if (!declaredExchanges.Contains(exchange))
                        throw new Exception($"Exchange {exchange} was not declared!");

                    foreach (string queue in exchangesToQueue[exchange])
                    {
                        string consumerTag = queueToConsumer[queue];
                        AsyncEventingBasicConsumer consumer = (AsyncEventingBasicConsumer)consumers[consumerTag];
                        consumer.HandleBasicDeliver(consumerTag, 1, false, exchange, routing, basicProperties, body);
                    }
                });

        RabbitMQPubSub sut = new RabbitMQPubSub(channel.Object, new Config(), new ExchangeNameFactory(new JsonSerializer()),
            new QueueNameFactory(), new JsonSerializer());
        
        return sut;
    }
}
