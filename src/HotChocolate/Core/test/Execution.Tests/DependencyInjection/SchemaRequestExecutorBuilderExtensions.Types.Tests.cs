using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;

namespace HotChocolate.Execution.DependencyInjection;

public class SchemaRequestExecutorBuilderExtensionsTypesTests
{
    [Fact]
    public async Task AddObjectType_TIsSchemaType()
    {
        (await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType()
                    .AddObjectType<ObjectType>()
                    .BuildSchemaAsync()))
            .Message
            .MatchSnapshot();
    }

    [Fact]
    public async Task AddObjectType_Configure_TIsSchemaType()
    {
        (await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType()
                    .AddObjectType<ObjectType>(d => { })
                    .BuildSchemaAsync()))
            .Message
            .MatchSnapshot();
    }

    [Fact]
    public async Task AddInterfaceType_TIsSchemaType()
    {
        (await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType()
                    .AddInterfaceType<ObjectType>()
                    .BuildSchemaAsync()))
            .Message
            .MatchSnapshot();
    }

    [Fact]
    public async Task AddInterfaceType_Configure_TIsSchemaType()
    {
        (await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType()
                    .AddInterfaceType<ObjectType>(d => { })
                    .BuildSchemaAsync()))
            .Message
            .MatchSnapshot();
    }

    [Fact]
    public async Task AddUnionType_TIsSchemaType()
    {
        (await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType()
                    .AddUnionType<ObjectType>()
                    .BuildSchemaAsync()))
            .Message
            .MatchSnapshot();
    }

    [Fact]
    public async Task AddUnionType_Configure_TIsSchemaType()
    {
        (await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType()
                    .AddObjectType<ObjectType>(d => { })
                    .BuildSchemaAsync()))
            .Message
            .MatchSnapshot();
    }

    [Fact]
    public async Task AddEnumType_TIsSchemaType()
    {
        (await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType()
                    .AddEnumType<ObjectType>()
                    .BuildSchemaAsync()))
            .Message
            .MatchSnapshot();
    }

    [Fact]
    public async Task AddEnumType_Configure_TIsSchemaType()
    {
        (await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType()
                    .AddEnumType<ObjectType>(d => { })
                    .BuildSchemaAsync()))
            .Message
            .MatchSnapshot();
    }

    [Fact]
    public async Task AddInputObjectType_TIsSchemaType()
    {
        (await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType()
                    .AddInputObjectType<ObjectType>()
                    .BuildSchemaAsync()))
            .Message
            .MatchSnapshot();
    }

    [Fact]
    public async Task AddInputObjectType_Configure_TIsSchemaType()
    {
        (await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType()
                    .AddInputObjectType<ObjectType>(d => { })
                    .BuildSchemaAsync()))
            .Message
            .MatchSnapshot();
    }
}
