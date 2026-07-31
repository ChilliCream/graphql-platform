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
    public void AddNitro_Should_RegisterOneComposition_When_ItIsCalledTwice()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitro();
        builder.AddNitro();

        // assert
        DescribeCompositionRegistrations(builder).MatchInlineSnapshot(
            "IDistributedApplicationEventingSubscriber -> SchemaComposition (Singleton)");
    }

    [Fact]
    public void AddNitro_Should_LeaveTheCoordinatorOut_When_TheStageIsNotProvided()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitro();

        // assert
        Assert.Null(GetNitroCompositionOptions(builder).Coordinator);
    }

    [Fact]
    public void AddNitro_Should_ConnectTheComposition_When_LocalCompositionWasAddedFirst()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitro();
        builder.AddNitro("production");

        // assert
        Assert.Equal("production", GetNitroCompositionOptions(builder).Coordinator?.Stage);
    }

    [Fact]
    public void AddNitro_Should_KeepTheCoordinator_When_LocalCompositionIsAddedAfterTheStage()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitro("production");
        builder.AddNitro();

        // assert
        Assert.Equal("production", GetNitroCompositionOptions(builder).Coordinator?.Stage);
    }

    [Fact]
    public void AddGraphQLOrchestrator_Should_DelegateToAddNitro()
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
    public void AddNitro_Should_KeepTheStage_When_ItIsCalledTwiceForTheSameStage()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitro("production");
        builder.AddNitro("production");

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
    public void AddNitro_Should_Throw_When_ItIsCalledTwiceForDifferentStages()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitro("production");

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddNitro("staging"));

        // assert
        Assert.Equal(
            "Nitro is already added for the stage 'production'. A distributed application "
            + "composes against a single stage, so AddNitro cannot be called again for the stage "
            + "'staging'.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AddNitro_Should_Throw_When_TheStageIsNotAName(string? stage)
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var exception = Record.Exception(() => builder.AddNitro(stage!));

        // assert
        Assert.Equal("stage", Assert.IsAssignableFrom<ArgumentException>(exception).ParamName);
    }

    [Fact]
    public void WithNitroApiId_Should_SelectTheApi_When_ItIsCalledOnAGateway()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
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
    public void WithNitroSchemaValidation_Should_AddTheOptInAnnotation_When_NitroIsConfigured()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitro("production");
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithNitroApiId("QXBpCmdhdGV3YXk");

        // act
        gateway.WithNitroSchemaValidation();

        // assert
        Assert.True(gateway.Resource.HasNitroSchemaValidation());
    }

    [Fact]
    public void WithNitroSchemaValidation_Should_Throw_When_TheStageIsMissing()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithNitroApiId("QXBpCmdhdGV3YXk");

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => gateway.WithNitroSchemaValidation());

        // assert
        Assert.Equal(
            "Nitro schema validation requires AddNitro(stage) to be called before "
            + "WithNitroSchemaValidation.",
            exception.Message);
    }

    [Fact]
    public void WithNitroSchemaValidation_Should_Throw_When_TheApiIdIsMissing()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitro("production");
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => gateway.WithNitroSchemaValidation());

        // assert
        Assert.Equal(
            "Nitro schema validation requires WithNitroApiId(apiId) to be configured first.",
            exception.Message);
    }

    [Fact]
    public void WithNitroSchemaValidation_Should_Throw_When_CompositionIsMissing()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitro("production");
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroApiId("QXBpCmdhdGV3YXk");

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => gateway.WithNitroSchemaValidation());

        // assert
        Assert.Equal(
            "Nitro schema validation can only be enabled on a gateway configured with "
            + "WithGraphQLSchemaComposition.",
            exception.Message);
    }

    [Fact]
    public void WithGraphQLSchemaComposition_Should_RegisterTheRecomposeCommand()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition();

        // assert
        var command = Assert.Single(
            gateway.Resource.Annotations.OfType<ResourceCommandAnnotation>(),
            annotation => annotation.Name == "recompose");
        Assert.Equal("Recompose", command.DisplayName);
        Assert.Equal("ArrowSync", command.IconName);
    }

    [Fact]
    public async Task WithGraphQLSchemaComposition_Should_ReturnControlledFailure_When_CompositionIsNotRegistered()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition();
        var command = Assert.Single(
            gateway.Resource.Annotations.OfType<ResourceCommandAnnotation>(),
            annotation => annotation.Name == "recompose");
        await using var services = new ServiceCollection().BuildServiceProvider();
#pragma warning disable ASPIREINTERACTION001
        var context = new ExecuteCommandContext
        {
            ServiceProvider = services,
            ResourceName = gateway.Resource.Name,
            CancellationToken = TestContext.Current.CancellationToken,
            Logger = NullLogger.Instance,
            Arguments = new InteractionInputCollection([])
        };
#pragma warning restore ASPIREINTERACTION001

        // act
        var result = await command.ExecuteCommand(context);

        // assert
        Assert.Equal(
            "False|Schema composition is not ready.",
            $"{result.Success}|{result.Message}");
    }

    [Fact]
    public void AddNitro_Should_StoreTheCallerSuppliedPortalUrl()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var portalUrl = new Uri("https://portal.example.test/custom?tenant=abc");

        // act
        builder.AddNitro("production", portalUrl);

        // assert
        Assert.Same(portalUrl, GetNitroCompositionOptions(builder).PortalUrl);
    }

    [Fact]
    public void AddNitro_Should_ConfigureSeedUpdates()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitro(
            "production",
            portalUrl: null,
            configureSeedUpdates: options =>
            {
                options.Enabled = false;
                options.AutoUpdate = false;
            });

        // assert
        var options = GetNitroCompositionOptions(builder).SeedUpdates;
        Assert.Equal("False|False", $"{options.Enabled}|{options.AutoUpdate}");
    }

    [Fact]
    public void AddNitro_Should_AcceptAnExplicitNullPortalUrl()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        builder.AddNitro("production", null);

        // assert
        Assert.Equal("production", GetNitroCompositionOptions(builder).Coordinator?.Stage);
    }

    [Fact]
    public void AddNitro_Should_UpdateAutoUpdateDefault_WhenCalledAgain()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddNitro("production");

        // act
        builder.AddNitro(
            "production",
            portalUrl: null,
            configureSeedUpdates: options => options.AutoUpdate = false);

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
        builder.AddNitro("production");

        // act
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
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
            .WithGraphQLSchemaComposition()
            .WithNitroApiId("QXBpCmdhdGV3YXk");

        // act
        builder.AddNitro("production");

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
        builder.AddNitro(
            "production",
            portalUrl: null,
            configureSeedUpdates: options => options.Enabled = false);
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
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
        builder.AddNitro("production");
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
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
