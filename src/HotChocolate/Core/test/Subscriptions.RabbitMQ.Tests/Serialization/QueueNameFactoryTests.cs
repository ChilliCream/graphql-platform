using System;
using Xunit;

namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public class QueueNameFactoryTests
{
    [Fact]
    public void Create()
    {
        string result = Sut().Create("exchange", "main-app");
        Assert.Equal("main-app - exchange", result);
    }

    [Fact]
    public void Create_Null()
    {
        Assert.Throws<ArgumentNullException>(() => Sut().Create(null!, "main-app"));
        Assert.Throws<ArgumentNullException>(() => Sut().Create("exhange", null!));
    }

    private IQueueNameFactory Sut()
        => new QueueNameFactory();
}
