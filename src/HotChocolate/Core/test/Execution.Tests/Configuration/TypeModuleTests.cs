using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Execution.Configuration;

public class TypeModuleTests
{
    [Fact]
    public async Task Use_Type_Module_From_DI()
    {
        await new ServiceCollection()
            .AddSingleton<DummyTypeModule>()
            .AddGraphQLServer()
            .AddTypeModule<DummyTypeModule>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Use_Type_Module_From_DI_And_Execute()
    {
        await new ServiceCollection()
            .AddSingleton<DummyTypeModule>()
            .AddGraphQLServer()
            .AddTypeModule<DummyTypeModule>()
            .ExecuteRequestAsync("{ hello }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Extend_Type_From_TypeModule()
    {
        await new ServiceCollection()
            .AddSingleton<DummyTypeModule>()
            .AddGraphQLServer()
            .AddTypeExtension<Query>()
            .AddTypeModule<DummyTypeModule>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Extend_Type_From_TypeModule_Execute()
    {
        await new ServiceCollection()
            .AddSingleton<DummyTypeModule>()
            .AddGraphQLServer()
            .AddTypeExtension<Query>()
            .AddTypeModule<DummyTypeModule>()
            .ExecuteRequestAsync("{ hello person { name dynamic } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Use_Type_Module_From_Factory()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddTypeModule(_ => new DummyTypeModule())
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Warmups_Are_Triggered_An_Appropriate_Number_Of_Times()
    {
        // arrange
        var typeModule = new TriggerableTypeModule();
        var warmups = 0;
        var warmupResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .AddTypeModule(_ => typeModule)
            .InitializeOnStartup(keepWarm: true, warmup: (_, _) =>
            {
                warmups++;
                warmupResetEvent.Set();
                return Task.CompletedTask;
            })
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var provider = services.BuildServiceProvider();
        var warmupService = provider.GetRequiredService<IHostedService>();

        _ = Task.Run(async () =>
        {
            await warmupService.StartAsync(CancellationToken.None);
        }, cts.Token);

        await provider.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();

        // act
        // assert
        warmupResetEvent.Wait(cts.Token);
        warmupResetEvent.Reset();

        Assert.Equal(1, warmups);

        typeModule.TriggerChange();
        warmupResetEvent.Wait(cts.Token);
        warmupResetEvent.Reset();

        Assert.Equal(2, warmups);

        typeModule.TriggerChange();
        warmupResetEvent.Wait(cts.Token);
        warmupResetEvent.Reset();

        Assert.Equal(3, warmups);
    }

    private sealed class TriggerableTypeModule : TypeModule
    {
        public void TriggerChange() => OnTypesChanged();
    }

    public class DummyTypeModule : ITypeModule
    {
#pragma warning disable CS0067
        public event EventHandler<EventArgs>? TypesChanged;
#pragma warning restore CS0067

        public ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
            IDescriptorContext context,
            CancellationToken cancellationToken)
        {
            var list = new List<ITypeSystemMember>();

            var typeDefinition = new ObjectTypeConfiguration("Query");
            typeDefinition.Fields.Add(new(
                "hello",
                type: TypeReference.Parse("String!"),
                pureResolver: _ => "world"));
            list.Add(ObjectType.CreateUnsafe(typeDefinition));

            var typeExtensionDefinition = new ObjectTypeConfiguration("Person");
            typeExtensionDefinition.Fields.Add(new(
                "dynamic",
                type: TypeReference.Parse("String!"),
                pureResolver: _ => "value"));
            list.Add(ObjectTypeExtension.CreateUnsafe(typeExtensionDefinition));

            return new(list);
        }
    }

    [ExtendObjectType("Query")]
    public class Query
    {
        public Person GetPerson() => new();
    }

    public class Person
    {
        public string Name => "Doe";
    }
}
