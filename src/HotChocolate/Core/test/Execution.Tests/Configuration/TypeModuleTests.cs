using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;

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

            var typeDefinition = new ObjectTypeDefinition("Query");
            typeDefinition.Fields.Add(new(
                "hello",
                type: TypeReference.Parse("String!"),
                pureResolver: _ => "world"));
            list.Add(ObjectType.CreateUnsafe(typeDefinition));

            var typeExtensionDefinition = new ObjectTypeDefinition("Person");
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
