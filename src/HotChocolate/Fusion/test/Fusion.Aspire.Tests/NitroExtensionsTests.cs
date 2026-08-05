using System.Runtime.CompilerServices;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using HotChocolate.Fusion.Aspire.Nitro;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

public sealed class NitroExtensionsTests
{
    [Fact]
    public void AddNitroComposition_Should_RegisterOneComposition_When_ItIsCalledTwice()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitroComposition();
        builder.AddNitroComposition();

        // assert
        DescribeCompositionRegistrations(builder).MatchInlineSnapshot(
            "IDistributedApplicationEventingSubscriber -> SchemaComposition (Singleton)");
    }

    [Fact]
    public void AddNitroComposition_Should_LeaveTheCoordinatorOut_When_TheStageIsNotProvided()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitroComposition();

        // assert
        Assert.Null(GetNitroCompositionOptions(builder).Coordinator);
    }

    [Fact]
    public void AddNitroComposition_Should_ConnectTheComposition_When_LocalCompositionWasAddedFirst()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitroComposition();
        builder.AddNitroComposition("production");

        // assert
        Assert.Equal("production", GetNitroCompositionOptions(builder).Coordinator?.Stage);
    }

    [Fact]
    public void AddNitroComposition_Should_KeepTheCoordinator_When_LocalCompositionIsAddedAfterTheStage()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitroComposition("production");
        builder.AddNitroComposition();

        // assert
        Assert.Equal("production", GetNitroCompositionOptions(builder).Coordinator?.Stage);
    }

    [Fact]
    public void AddGraphQLOrchestrator_Should_DelegateToAddNitroComposition()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
#pragma warning disable CS0618 // Verify the compatibility shim.
        var result = builder.AddGraphQLOrchestrator();
#pragma warning restore CS0618

        // assert
        Assert.Same(builder, result);
        Assert.Null(GetNitroCompositionOptions(builder).Coordinator);
        DescribeCompositionRegistrations(builder).MatchInlineSnapshot(
            "IDistributedApplicationEventingSubscriber -> SchemaComposition (Singleton)");
    }

    [Fact]
    public void AddNitroComposition_Should_KeepTheStage_When_ItIsCalledTwiceForTheSameStage()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitroComposition("production");
        builder.AddNitroComposition("production");

        // assert
        $"""
        {DescribeCompositionRegistrations(builder)}
        Stage: {GetNitroCompositionOptions(builder).Coordinator?.Stage}
        """.MatchInlineSnapshot(
            """
            IDistributedApplicationEventingSubscriber -> SchemaComposition (Singleton)
            Stage: production
            """);
    }

    [Fact]
    public void AddNitroComposition_Should_Throw_When_ItIsCalledTwiceForDifferentStages()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitroComposition("production");

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddNitroComposition("staging"));

        // assert
        Assert.Equal(
            "Nitro is already added for the stage 'production'. A distributed application "
            + "composes against a single stage, so AddNitroComposition cannot be called again "
            + "for the stage 'staging'.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void AddNitroComposition_Should_Throw_When_TheStageIsNotAName(string stage)
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var exception = Record.Exception(() => builder.AddNitroComposition(stage));

        // assert
        Assert.Equal("stage", Assert.IsAssignableFrom<ArgumentException>(exception).ParamName);
    }

    [Fact]
    public void AddNitroComposition_Should_Throw_When_ThePortalUrlIsGivenWithoutAStage()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var exception = Record.Exception(
            () => builder.AddNitroComposition(portalUrl: new Uri("https://portal.example.test")));

        // assert
        Assert.Equal(
            "portalUrl|The Nitro portal URL can only be set together with a stage. "
            + "(Parameter 'portalUrl')",
            $"{Assert.IsType<ArgumentException>(exception).ParamName}|{exception.Message}");
    }

    [Fact]
    public void AddNitroComposition_Should_Throw_When_TheSeedUpdatesAreGivenWithoutAStage()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var exception = Record.Exception(
            () => builder.AddNitroComposition(seedUpdates: new NitroSeedUpdateOptions { Enabled = false }));

        // assert
        Assert.Equal(
            "seedUpdates|The Nitro seed update settings can only be set together with a stage. "
            + "(Parameter 'seedUpdates')",
            $"{Assert.IsType<ArgumentException>(exception).ParamName}|{exception.Message}");
    }

    [Fact]
    public void WithNitroApiId_Should_SelectTheApi_When_ItIsCalledOnAGateway()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithNitroApiId("QXBpCmdhdGV3YXk");

        // assert
        Assert.Equal("QXBpCmdhdGV3YXk", gateway.Resource.GetNitroApiId());
    }

    [Fact]
    public void WithNitroApiId_Should_KeepTheLastApiId_When_ItIsCalledTwice()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroApiId("QXBpCmZpcnN0")
            .WithNitroApiId("QXBpCnNlY29uZA");

        // assert
        Assert.Equal(
            ["QXBpCnNlY29uZA"],
            gateway.Resource.Annotations
                .OfType<NitroApiIdAnnotation>()
                .Select(annotation => annotation.ApiId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void WithNitroApiId_Should_Throw_When_TheApiIdIsNotAnId(string? apiId)
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var gateway = builder.AddProject("gateway", GetTestProjectFile());

        // act
        var exception = Record.Exception(() => gateway.WithNitroApiId(apiId!));

        // assert
        Assert.Equal("apiId", Assert.IsAssignableFrom<ArgumentException>(exception).ParamName);
    }

    [Fact]
    public void WithNitroComposition_Should_RegisterTheRecomposeCommand()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition();

        // assert
        var command = Assert.Single(
            gateway.Resource.Annotations.OfType<ResourceCommandAnnotation>(),
            annotation => annotation.Name == "recompose");
        Assert.Equal("Recompose", command.DisplayName);
        Assert.Equal("ArrowSync", command.IconName);
    }

    [Fact]
    public async Task RecomposeCommand_Should_UseLogicalResourceName_When_ExecutingRuntimeInstance()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition();
        var command = Assert.Single(
            gateway.Resource.Annotations.OfType<ResourceCommandAnnotation>(),
            annotation => annotation.Name == "recompose");
        var coordinator = new GatewayCompositionCommandCoordinator();
        coordinator.Register(
            gateway.Resource.Name,
            _ => Task.FromResult(CommandResults.Success("Schema composition completed")));
        await using var services = new ServiceCollection()
            .AddSingleton(coordinator)
            .BuildServiceProvider();
        var context = new ExecuteCommandContext
        {
            ServiceProvider = services,
            ResourceName = "gateway-runtime-instance",
            CancellationToken = TestContext.Current.CancellationToken,
            Logger = NullLogger.Instance,
            Arguments = new InteractionInputCollection([])
        };

        // act
        var result = await command.ExecuteCommand(context);

        // assert
        Assert.Equal(
            "True|Schema composition completed",
            $"{result.Success}|{result.Message}");
    }

    [Fact]
    public async Task WithNitroComposition_Should_ReturnControlledFailure_When_CompositionIsNotRegistered()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition();
        var command = Assert.Single(
            gateway.Resource.Annotations.OfType<ResourceCommandAnnotation>(),
            annotation => annotation.Name == "recompose");
        await using var services = new ServiceCollection().BuildServiceProvider();
        var context = new ExecuteCommandContext
        {
            ServiceProvider = services,
            ResourceName = gateway.Resource.Name,
            CancellationToken = TestContext.Current.CancellationToken,
            Logger = NullLogger.Instance,
            Arguments = new InteractionInputCollection([])
        };

        // act
        var result = await command.ExecuteCommand(context);

        // assert
        Assert.Equal(
            "False|Schema composition is not ready.",
            $"{result.Success}|{result.Message}");
    }

    [Fact]
    public void AddNitroComposition_Should_StoreTheCallerSuppliedPortalUrl()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var portalUrl = new Uri("https://portal.example.test/custom?tenant=abc");

        // act
        builder.AddNitroComposition("production", portalUrl);

        // assert
        Assert.Same(portalUrl, GetNitroCompositionOptions(builder).PortalUrl);
    }

    [Fact]
    public void AddNitroComposition_Should_ConfigureSeedUpdates()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitroComposition(
            "production",
            seedUpdates: new NitroSeedUpdateOptions { Enabled = false, AutoUpdate = false });

        // assert
        var options = GetNitroCompositionOptions(builder).SeedUpdates;
        Assert.Equal("False|False", $"{options.Enabled}|{options.AutoUpdate}");
    }

    [Fact]
    public void AddNitroComposition_Should_AcceptAnExplicitNullPortalUrl()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitroComposition("production", null);

        // assert
        Assert.Equal("production", GetNitroCompositionOptions(builder).Coordinator?.Stage);
    }

    [Fact]
    public void AddNitroComposition_Should_UpdateAutoUpdateDefault_WhenCalledAgain()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitroComposition("production");

        // act
        builder.AddNitroComposition(
            "production",
            seedUpdates: new NitroSeedUpdateOptions { AutoUpdate = false });

        // assert
        var options = GetNitroCompositionOptions(builder);
        Assert.Equal(
            "False|False",
            $"{options.SeedUpdates.AutoUpdate}|"
            + $"{options.Coordinator!.IsAutoUpdateEnabled("gateway")}");
    }

    [Fact]
    public void NitroGateway_Should_RegisterBothAutoUpdateCommands_WhenNitroIsAddedFirst()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitroComposition("production");

        // act
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithNitroApiId("QXBpCmdhdGV3YXk");

        // assert
        string.Join(
                Environment.NewLine,
                gateway.Resource.Annotations
                    .OfType<ResourceCommandAnnotation>()
                    .Select(command => $"{command.Name}: {command.DisplayName}")
                    .Order(StringComparer.Ordinal))
            .MatchInlineSnapshot(
                """
                disable-nitro-auto-update: Disable auto-update
                enable-nitro-auto-update: Enable auto-update
                recompose: Recompose
                """);
    }

    [Fact]
    public void NitroGateway_Should_RegisterBothAutoUpdateCommands_WhenNitroIsAddedLast()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithNitroApiId("QXBpCmdhdGV3YXk");

        // act
        builder.AddNitroComposition("production");

        // assert
        Assert.Equal(
            2,
            gateway.Resource.Annotations
                .OfType<ResourceCommandAnnotation>()
                .Count(command => command.Name.Contains("nitro-auto-update", StringComparison.Ordinal)));
    }

    [Fact]
    public void SeedUpdateService_Should_NotStartMonitor_WhenDetectionIsDisabled()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitroComposition(
            "production",
            seedUpdates: new NitroSeedUpdateOptions { Enabled = false });
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithNitroApiId("QXBpCmdhdGV3YXk");
        var lifetime = new TestHostApplicationLifetime();
        var resourceLoggerService = new ResourceLoggerService();
        var service = new NitroSeedUpdateService(
            GetNitroCompositionOptions(builder),
            resourceLoggerService,
            NoopSeedUpdateNotifier.Instance,
            lifetime,
            NullLoggerFactory.Instance,
            TimeProvider.System);
        using var gate = new SemaphoreSlim(1, 1);

        // act
        service.Start(
            gateway.Resource,
            "QXBpCmdhdGV3YXk",
            gate,
            (_, _) => Task.FromResult(true));

        // assert
        Assert.Equal(0, service.MonitorCount);
    }

    [Fact]
    public async Task AutoUpdateCommands_Should_HideUntilMonitorIsReady()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitroComposition("production");
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithNitroApiId("QXBpCmdhdGV3YXk");
        var lifetime = new TestHostApplicationLifetime();
        var service = new NitroSeedUpdateService(
            GetNitroCompositionOptions(builder),
            new ResourceLoggerService(),
            NoopSeedUpdateNotifier.Instance,
            lifetime,
            NullLoggerFactory.Instance,
            TimeProvider.System);
        await using var services = new ServiceCollection()
            .AddSingleton(service)
            .BuildServiceProvider();
        var commands = gateway.Resource.Annotations
            .OfType<ResourceCommandAnnotation>()
            .Where(command => command.Name.Contains("nitro-auto-update", StringComparison.Ordinal))
            .OrderBy(command => command.Name, StringComparer.Ordinal)
            .ToArray();
        var context = new UpdateCommandStateContext
        {
            ResourceSnapshot = new CustomResourceSnapshot
            {
                ResourceType = "project",
                Properties = []
            },
            ServiceProvider = services
        };

        // act
        var beforeStart = commands.Select(command => command.UpdateState!(context)).ToArray();
        lifetime.StopApplication();
        using var gate = new SemaphoreSlim(1, 1);
        service.Start(
            gateway.Resource,
            "QXBpCmdhdGV3YXk",
            gate,
            (_, _) => Task.FromResult(true));
        var afterStart = commands.Select(command => command.UpdateState!(context)).ToArray();

        // assert
        Assert.Equal("Hidden, Hidden", string.Join(", ", beforeStart));
        Assert.Equal("Enabled, Hidden", string.Join(", ", afterStart));
    }

    [Fact]
    public async Task DisableAutoUpdateCommand_Should_UseLogicalResourceName_When_ExecutingRuntimeInstance()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitroComposition("production");
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithNitroApiId("QXBpCmdhdGV3YXk");
        var lifetime = new TestHostApplicationLifetime();
        var service = new NitroSeedUpdateService(
            GetNitroCompositionOptions(builder),
            new ResourceLoggerService(),
            NoopSeedUpdateNotifier.Instance,
            lifetime,
            NullLoggerFactory.Instance,
            TimeProvider.System);
        lifetime.StopApplication();
        using var gate = new SemaphoreSlim(1, 1);
        service.Start(
            gateway.Resource,
            "QXBpCmdhdGV3YXk",
            gate,
            (_, _) => Task.FromResult(true));
        await using var services = new ServiceCollection()
            .AddSingleton(service)
            .BuildServiceProvider();
        var command = Assert.Single(
            gateway.Resource.Annotations.OfType<ResourceCommandAnnotation>(),
            annotation => annotation.Name == "disable-nitro-auto-update");
        var context = new ExecuteCommandContext
        {
            ServiceProvider = services,
            ResourceName = "gateway-runtime-instance",
            CancellationToken = TestContext.Current.CancellationToken,
            Logger = NullLogger.Instance,
            Arguments = new InteractionInputCollection([])
        };

        // act
        var result = await command.ExecuteCommand(context);

        // assert
        Assert.Equal(
            "True|Automatic Nitro updates disabled",
            $"{result.Success}|{result.Message}");
    }

    /// <summary>
    /// Describes every registration of the schema composition. The distributed application
    /// registers eventing subscribers of its own, so only the registrations of the composition
    /// are described.
    /// </summary>
    private static string DescribeCompositionRegistrations(IDistributedApplicationBuilder builder)
        => string.Join(
            Environment.NewLine,
            builder.Services
                .Where(descriptor => descriptor.ImplementationType == typeof(SchemaComposition))
                .Select(descriptor =>
                    $"{descriptor.ServiceType.Name} -> {descriptor.ImplementationType!.Name} "
                    + $"({descriptor.Lifetime})"));

    private static NitroCompositionOptions GetNitroCompositionOptions(
        IDistributedApplicationBuilder builder)
        => (NitroCompositionOptions)Assert.Single(
                builder.Services,
                descriptor => descriptor.ServiceType == typeof(NitroCompositionOptions))
            .ImplementationInstance!;

    private static string GetTestProjectFile([CallerFilePath] string sourceFile = "")
        => IOPath.Combine(
            IOPath.GetDirectoryName(sourceFile)!,
            "HotChocolate.Fusion.Aspire.Tests.csproj");

    private sealed class NoopSeedUpdateNotifier : INitroSeedUpdateNotifier
    {
        public static NoopSeedUpdateNotifier Instance { get; } = new();

        public void NotifyAdopted(string message)
        {
        }

        public void NotifyStaged(string message)
        {
        }
    }
}
