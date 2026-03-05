using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class HostInfoTests
{
    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    [Fact]
    public void Runtime_Should_Have_Host_Info_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.NotNull(runtime.Host);
    }

    [Fact]
    public void HostInfo_Should_Have_MachineName_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert — MachineName should match the current machine
        Assert.Equal(Environment.MachineName, runtime.Host.MachineName);
    }

    [Fact]
    public void HostInfo_Should_Have_ProcessName_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert — ProcessName should match the current process
        using var process = Process.GetCurrentProcess();
        Assert.Equal(process.ProcessName, runtime.Host.ProcessName);
    }

    [Fact]
    public void HostInfo_Should_Have_ProcessId_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.True(runtime.Host.ProcessId > 0);
    }

    [Fact]
    public void HostInfo_Should_Have_FrameworkVersion_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert — FrameworkVersion should match the runtime framework description
        Assert.Equal(RuntimeInformation.FrameworkDescription, runtime.Host.FrameworkVersion);
    }

    [Fact]
    public void HostInfo_Should_Have_OperatingSystemVersion_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert — OperatingSystemVersion should match the OS description
        Assert.Equal(RuntimeInformation.OSDescription, runtime.Host.OperatingSystemVersion);
    }

    [Fact]
    public void HostInfo_Should_Have_EnvironmentName_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.NotNull(runtime.Host.EnvironmentName);
        Assert.NotEmpty(runtime.Host.EnvironmentName);
    }

    [Fact]
    public void HostInfo_Should_Have_InstanceId_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.NotEqual(Guid.Empty, runtime.Host.InstanceId);
    }

    [Fact]
    public void HostInfo_Should_Have_RuntimeInfo_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.NotNull(runtime.Host.RuntimeInfo);
    }

    [Fact]
    public void RuntimeInfo_Should_Have_ProcessorCount_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.True(runtime.Host.RuntimeInfo.ProcessorCount > 0);
    }

    [Fact]
    public void RuntimeInfo_Should_Have_RuntimeIdentifier_When_Created()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert — RuntimeIdentifier should match the runtime identifier
        Assert.Equal(RuntimeInformation.RuntimeIdentifier, runtime.Host.RuntimeInfo.RuntimeIdentifier);
    }

    [Fact]
    public void HostInfo_Should_Allow_MachineName_Override_When_Configured()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).Host(d => d.MachineName("custom-machine")));
        });

        // assert
        Assert.Equal("custom-machine", runtime.Host.MachineName);
    }

    [Fact]
    public void HostInfo_Should_Allow_ServiceName_Override_When_Configured()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).Host(d => d.ServiceName("my-test-service")));
        });

        // assert
        Assert.Equal("my-test-service", runtime.Host.ServiceName);
    }

    [Fact]
    public void HostInfo_Should_Allow_InstanceId_Override_When_Configured()
    {
        // arrange
        var customId = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).Host(d => d.InstanceId(customId)));
        });

        // assert
        Assert.Equal(customId, runtime.Host.InstanceId);
    }

    [Fact]
    public void HostInfo_Should_Allow_ProcessId_Override_When_Configured()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).Host(d => d.ProcessId(99999)));
        });

        // assert
        Assert.Equal(99999, runtime.Host.ProcessId);
    }

    [Fact]
    public void Runtimes_Should_Have_Different_InstanceIds_When_Created()
    {
        // arrange & act
        var runtime1 = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var runtime2 = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.NotEqual(runtime1.Host.InstanceId, runtime2.Host.InstanceId);
    }

    public sealed class OrderCreated
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
    }
}
