using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using Moq;
using Xunit;

namespace HotChocolate.Subscriptions.Nats;

public class NatsSubscriptionsServiceCollectionExtensionsTests
{
    // [Fact]
    // public void AddNatsSubscriptions_1_Services_Is_Null()
    // {
    //     static void Fail()
    //         => default(IServiceCollection)
    //             .AddNatsSubscriptions();
    //
    //     Assert.Throws<ArgumentNullException>(Fail);
    // }
    //
    // [Fact]
    // public void AddNatsSubscriptions_1_Connection_Is_Null()
    // {
    //     static void Fail() =>new ServiceCollection()
    //         .AddNatsSubscriptions();
    //     Assert.Throws<ArgumentNullException>(Fail);
    // }
    //
    // [Fact]
    // public void AddRedisSubscriptions_2_Services_Is_Null()
    // {
    //     static void Fail() =>
    //         default(IRequestExecutorBuilder).AddRedisSubscriptions(
    //             sp => sp.GetRequiredService<IConnectionMultiplexer>());
    //
    //     Assert.Throws<ArgumentNullException>(Fail);
    // }
    //
    // [Fact]
    // public void AddRedisSubscriptions_2_Connection_Is_Null()
    // {
    //     var builder = new Mock<IRequestExecutorBuilder>();
    //     void Fail() => builder.Object.AddRedisSubscriptions(default);
    //     Assert.Throws<ArgumentNullException>(Fail);
    // }
    //
    // [Fact]
    // public void AddRedisSubscriptions_3_Services_Is_Null()
    // {
    //     void Fail() => default(IRequestExecutorBuilder).AddRedisSubscriptions();
    //     Assert.Throws<ArgumentNullException>(Fail);
    // }
}
