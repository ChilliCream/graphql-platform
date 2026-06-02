using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class IntegrationTests
{
    [Fact]
    public async Task Schema_Snapshot()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Schema_Snapshot_Without_ConnectionName_Inference()
    {
        await new ServiceCollection()
            .AddGraphQLServer(disableDefaultSecurity: true)
            .AddIntegrationTestTypes()
            .AddGlobalObjectIdentification()
            .ModifyPagingOptions(o => o.InferConnectionNameFromField = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Subscription_With_Subscribe_With_Delivers_Message_From_Stream()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildRequestExecutorAsync();

        // act
        await using var subscriptionResult = await executor.ExecuteAsync(
            "subscription { onProductAdded(categoryId: 42) }");

        // assert
        var stream = subscriptionResult.ExpectResponseStream();
        await foreach (var result in stream.ReadResultsAsync())
        {
            result.MatchInlineSnapshot(
                """
                {
                  "data": {
                    "onProductAdded": 42
                  }
                }
                """);
            break;
        }
    }

    [Fact]
    public async Task Subscription_With_Public_Subscribe_Source_Is_Not_Exposed_As_Field()
    {
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync();

        var subscription = schema.Types.GetType<ObjectType>("Subscription");
        Assert.Equal(
            ["onProductAdded", "onProductPriceChanged"],
            subscription.Fields.Where(f => !f.IsIntrospectionField).Select(f => f.Name).ToArray());
    }

    [Fact]
    public async Task ObjectTypeDescriptorAttribute_Should_Receive_NonNull_Type_When_Applied_To_StaticPartial_TypeExtension()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync();

        // act
        var renamedType = schema.Types.GetType<ObjectType>("renamed_DescriptorAttributeProbe");

        // assert
        Assert.NotNull(renamedType);
    }

    [Fact]
    public async Task DeclaringType_Should_Tag_Only_Own_Partial_Fields_When_TypeSplitAcrossPartials()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync();

        // act
        var type = schema.Types.GetType<ObjectType>("DeclaringTypeProbe");

        // assert
        // TagOwnFieldsAttribute sits on partial A and tags only the fields whose
        // DeclaringType is partial A. Partial B's field and the entity property must stay untagged.
        Snapshot.Create()
            .Add(type.Fields["fromPartialA"].Description, "fromPartialA")
            .Add(type.Fields["fromPartialB"].Description, "fromPartialB")
            .Add(type.Fields["id"].Description, "id")
            .MatchInline(
                """
                fromPartialA
                ---------------
                tagged
                ---------------

                fromPartialB
                ---------------
                null
                ---------------

                id
                ---------------
                null
                ---------------

                """);
    }

    [Fact]
    public async Task Maps_NullOrdering_From_PagingOptions_To_PagingArguments()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .ModifyPagingOptions(o => o.NullOrdering = NullOrdering.NativeNullsFirst)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ ints { nodes } }");
        var operationResult = result.ExpectOperationResult();

        // assert
        Assert.Empty(operationResult.Errors);
        Assert.Equal(NullOrdering.NativeNullsFirst, Query.PagingArguments.NullOrdering);
    }

    [Fact]
    public async Task Resolves_Instance_Method_On_NonStatic_QueryType()
    {
        // arrange
        // NonStaticPagedQuery.SomeBooks returns a Book whose title is the resolver
        // instance's InstanceId (a 32-char hex GUID).
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ someBooks { nodes { title } } }");

        // assert
        var json = result.ToJson();
        Assert.DoesNotContain("\"errors\"", json);
        Assert.Matches("\"title\": \"[0-9a-f]{32}\"", json);
    }
}
