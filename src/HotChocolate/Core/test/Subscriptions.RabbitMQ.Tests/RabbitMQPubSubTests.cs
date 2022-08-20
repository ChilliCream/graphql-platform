using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Subscriptions.RabbitMQ.Configuration;
using HotChocolate.Subscriptions.RabbitMQ.Serialization;
using Moq;
using RabbitMQ.Client;
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
    public async Task SingleTopic()
    {
        RabbitMQPubSub sut = Sut();

        ISourceStream<Car> stream = await sut.SubscribeAsync<string, Car>("new-car");
        ValueTask<Car[]> task = stream.ReadEventsAsync().ToArrayAsync();

        await Sut().SendAsync("new-car", new Car { Id = 1, Brand = "Škoda" });
        await Sut().SendAsync("new-car", new Car { Id = 2, Brand = "Volkswagen" });
        await Sut().SendAsync("new-car", new Car { Id = 3, Brand = "Mercedes-Benz" });

        Car[] cars = await task;
        Assert.Equal(new[] {1,2,3}, cars.Select(c => c.Id));
        Assert.Equal(new[] { "Škoda", "Volkswagen", "Mercedes-Benz" }, cars.Select(c => c.Brand));
    }

    public RabbitMQPubSub Sut()
    {
        Mock<IModel> channel = new();
        RabbitMQPubSub sut = new RabbitMQPubSub(channel.Object, new Config(), new ExchangeNameFactory(new JsonSerializer()),
            new QueueNameFactory(), new JsonSerializer());
        return sut;
    }
}
