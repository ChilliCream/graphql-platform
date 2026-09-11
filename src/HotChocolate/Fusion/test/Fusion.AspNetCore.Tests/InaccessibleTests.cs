using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class InaccessibleTests : FusionTestBase
{
    [Fact]
    public async Task Inaccessible_Fields_Cannot_Be_Queried_Via_Introspection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "Author") {
                fields {
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Fields_Can_Be_Used_As_Requirements()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              bookById(id: 1) {
                author {
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Fields_Cannot_Be_Queried()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              bookById(id: 1) {
                author {
                  id
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Field_With_Inaccessible_Argument_Can_Be_Queried()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              inaccessibleText
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Field_With_Inaccessible_Argument_Cannot_Be_Passed_In()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              inaccessibleText(text: "foo")
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Enum_With_Inaccessible_Value_When_Accessible_Value_Is_Used()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              enumField(value: ITEM2)
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Enum_With_Inaccessible_Value_When_Inaccessible_Value_Is_Used()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              enumField(value: ITEM1)
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Input_With_Inaccessible_Field_When_Accessible_Field_Is_Passed()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              shareable(input: { a: "Hello" })
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Input_With_Inaccessible_Field_When_Inaccessible_Field_Is_Not_Passed()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              shareable(input: { b: "Hello" })
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Object_Type_Cannot_Be_Queried_Via_Introspection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "InternalMetadata") {
                name
                kind
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Object_Type_Cannot_Be_Queried()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              metadata {
                version
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    // TODO: Re-enable interface tests once structural issue is resolved
    // (inaccessible interface with inaccessible implementation causes schema validation error)
    // [Fact]
    // public async Task Inaccessible_Interface_Type_Cannot_Be_Queried_Via_Introspection()
    // {
    //     // arrange
    //     using var server1 = CreateSourceSchema(
    //         "a",
    //         b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>());
    //
    //     using var server2 = CreateSourceSchema(
    //         "b",
    //         b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());
    //
    //     using var gateway = await CreateCompositeSchemaAsync(
    //     [
    //         ("a", server1),
    //         ("b", server2)
    //     ]);
    //
    //     // act
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //
    //     var request = new OperationRequest(
    //         """
    //         {
    //           __type(name: "InternalNode") {
    //             name
    //             kind
    //           }
    //         }
    //         """);
    //
    //     using var result = await client.PostAsync(
    //         request,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     await MatchSnapshotAsync(gateway, request, result);
    // }
    //
    // [Fact]
    // public async Task Inaccessible_Interface_Type_Cannot_Be_Used_In_Query()
    // {
    //     // arrange
    //     using var server1 = CreateSourceSchema(
    //         "a",
    //         b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>());
    //
    //     using var server2 = CreateSourceSchema(
    //         "b",
    //         b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());
    //
    //     using var gateway = await CreateCompositeSchemaAsync(
    //     [
    //         ("a", server1),
    //         ("b", server2)
    //     ]);
    //
    //     // act
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //
    //     var request = new OperationRequest(
    //         """
    //         {
    //           internalNode {
    //             internalId
    //           }
    //         }
    //         """);
    //
    //     using var result = await client.PostAsync(
    //         request,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     await MatchSnapshotAsync(gateway, request, result);
    // }

    // TODO: Re-enable union tests once union type definition is fixed
    // [Fact]
    // public async Task Inaccessible_Union_Type_Cannot_Be_Queried_Via_Introspection()
    // {
    //     // arrange
    //     using var server1 = CreateSourceSchema(
    //         "a",
    //         b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>());
    //
    //     using var server2 = CreateSourceSchema(
    //         "b",
    //         b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());
    //
    //     using var gateway = await CreateCompositeSchemaAsync(
    //     [
    //         ("a", server1),
    //         ("b", server2)
    //     ]);
    //
    //     // act
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //
    //     var request = new OperationRequest(
    //         """
    //         {
    //           __type(name: "InternalResult") {
    //             name
    //             kind
    //           }
    //         }
    //         """);
    //
    //     using var result = await client.PostAsync(
    //         request,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     await MatchSnapshotAsync(gateway, request, result);
    // }
    //
    // [Fact]
    // public async Task Inaccessible_Union_Type_Cannot_Be_Queried()
    // {
    //     // arrange
    //     using var server1 = CreateSourceSchema(
    //         "a",
    //         b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>());
    //
    //     using var server2 = CreateSourceSchema(
    //         "b",
    //         b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());
    //
    //     using var gateway = await CreateCompositeSchemaAsync(
    //     [
    //         ("a", server1),
    //         ("b", server2)
    //     ]);
    //
    //     // act
    //     using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //
    //     var request = new OperationRequest(
    //         """
    //         {
    //           internalResult {
    //             __typename
    //           }
    //         }
    //         """);
    //
    //     using var result = await client.PostAsync(
    //         request,
    //         new Uri("http://localhost:5000/graphql"));
    //
    //     // assert
    //     await MatchSnapshotAsync(gateway, request, result);
    // }

    [Fact]
    public async Task Inaccessible_Scalar_Type_Cannot_Be_Queried_Via_Introspection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>()
                  .AddType<InaccessibleTypes.SourceSchema1.InternalScalarType>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "InternalScalar") {
                name
                kind
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Enum_Type_Cannot_Be_Queried_Via_Introspection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "InternalStatus") {
                name
                kind
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Enum_Type_Cannot_Be_Used()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              statusField(status: ACTIVE)
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Input_Object_Type_Cannot_Be_Queried_Via_Introspection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "InternalFilter") {
                name
                kind
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Input_Object_Type_Cannot_Be_Used()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleTypes.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              filterData(filter: { includeInternal: true })
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Argument_Not_In_Field_Introspection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "Query") {
                fields {
                  name
                  args {
                    name
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Enum_Value_Not_In_Enum_Introspection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "SomeEnum") {
                enumValues {
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inaccessible_Input_Field_Not_In_Input_Object_Introspection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<InaccessibleField.SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<InaccessibleField.SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "SomeInput") {
                inputFields {
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Fields_Should_BeInQueryIntrospection_When_MarkedInaccessibleAndGoiIsEnabled()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(
            applyInaccessibleToNodeFields: true,
            enableGlobalObjectIdentification: true);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "Query") {
                fields {
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Fields_Should_NotBeInQueryIntrospection_When_MarkedInaccessibleAndGoiIsDisabled()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(
            applyInaccessibleToNodeFields: true,
            enableGlobalObjectIdentification: false);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "Query") {
                fields {
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Field_Should_BeSelectable_When_MarkedInaccessibleAndGoiIsEnabled()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(
            applyInaccessibleToNodeFields: true,
            enableGlobalObjectIdentification: true);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              node(id: "UmV2aWV3OjE=") {
                __typename
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            result,
            results =>
            {
                var response = Assert.Single(results);
                Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);
                Assert.Equal(
                    """
                    {"node":{"__typename":"Review"}}
                    """,
                    response.Data.GetRawText());
            });
    }

    [Fact]
    public async Task Node_Field_Should_BeRejectedAsUnknownField_When_MarkedInaccessibleAndGoiIsDisabled()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(
            applyInaccessibleToNodeFields: true,
            enableGlobalObjectIdentification: false);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              node(id: "UmV2aWV3OjE=") {
                __typename
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Entities_Should_StillResolveAcrossSourceSchemas_When_NodeFieldIsMarkedInaccessibleAndGoiIsEnabled()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(
            applyInaccessibleToNodeFields: true,
            enableGlobalObjectIdentification: true);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              reviews {
                body
                stars
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            result,
            results =>
            {
                var response = Assert.Single(results);
                Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);
                Assert.Equal(
                    """
                    {"reviews":[{"body":"A great read","stars":5},{"body":"Could not put it down","stars":4}]}
                    """,
                    response.Data.GetRawText());
            });
    }

    [Fact]
    public async Task Entities_Should_StillResolveAcrossSourceSchemas_When_NodeFieldIsMarkedInaccessibleAndGoiIsDisabled()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(
            applyInaccessibleToNodeFields: true,
            enableGlobalObjectIdentification: false);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              reviews {
                body
                stars
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            result,
            results =>
            {
                var response = Assert.Single(results);
                Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);
                Assert.Equal(
                    """
                    {"reviews":[{"body":"A great read","stars":5},{"body":"Could not put it down","stars":4}]}
                    """,
                    response.Data.GetRawText());
            });
    }

    [Fact]
    public async Task Node_Fields_Should_BeInQueryIntrospection_When_NotMarkedInaccessible()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(applyInaccessibleToNodeFields: false);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              __type(name: "Query") {
                fields {
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Published_Schema_Should_NotHaveNodeField_When_NodeFieldIsMarkedInaccessibleAndGoiIsDisabled()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(
            applyInaccessibleToNodeFields: true,
            enableGlobalObjectIdentification: false);

        // act
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(
            "http://localhost:5000/graphql/schema",
            TestContext.Current.CancellationToken);
        var sdl = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.False(HasQueryField(sdl, "node"));
        Assert.False(HasQueryField(sdl, "nodes"));
        sdl.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Published_Schema_Should_HaveNodeField_When_NodeFieldIsMarkedInaccessibleAndGoiIsEnabled()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(
            applyInaccessibleToNodeFields: true,
            enableGlobalObjectIdentification: true);

        // act
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(
            "http://localhost:5000/graphql/schema",
            TestContext.Current.CancellationToken);
        var sdl = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(HasQueryField(sdl, "node"));
        Assert.False(HasQueryField(sdl, "nodes"));
        sdl.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Published_Schema_Should_HaveNodeField_When_NodeFieldIsNotMarkedInaccessibleAndGoiIsEnabled()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(
            applyInaccessibleToNodeFields: false,
            enableGlobalObjectIdentification: true);

        // act
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(
            "http://localhost:5000/graphql/schema",
            TestContext.Current.CancellationToken);
        var sdl = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(HasQueryField(sdl, "node"));
        sdl.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Published_Schema_Should_HaveNodeField_When_NodeFieldIsNotMarkedInaccessibleAndGoiIsDisabled()
    {
        // arrange
        using var gateway = await CreateInaccessibleNodeFieldGatewayAsync(
            applyInaccessibleToNodeFields: false,
            enableGlobalObjectIdentification: false);

        // act
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(
            "http://localhost:5000/graphql/schema",
            TestContext.Current.CancellationToken);
        var sdl = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(HasQueryField(sdl, "node"));
        sdl.MatchSnapshot(extension: ".graphql");
    }

    private static bool HasQueryField(string sdl, string fieldName)
    {
        var queryType = Utf8GraphQLParser.Parse(sdl).Definitions
            .OfType<ObjectTypeDefinitionNode>()
            .Single(t => t.Name.Value == "Query");

        return queryType.Fields.Any(f => f.Name.Value == fieldName);
    }

    private Task<Gateway> CreateInaccessibleNodeFieldGatewayAsync(
        bool applyInaccessibleToNodeFields,
        bool enableGlobalObjectIdentification = true)
    {
        var server1 = CreateSourceSchema(
            "a",
            b => b
                .AddQueryType<InaccessibleNodeField.SourceSchema1.Query>()
                .AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .ModifyOptions(o => o.ApplyInaccessibleToNodeFields = applyInaccessibleToNodeFields));

        var server2 = CreateSourceSchema(
            "b",
            b => b
                .AddQueryType<InaccessibleNodeField.SourceSchema2.Query>()
                .AddType<InaccessibleNodeField.SourceSchema2.Review>()
                .AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .ModifyOptions(o => o.ApplyInaccessibleToNodeFields = applyInaccessibleToNodeFields));

        return CreateCompositeSchemaAsync(
            [
                ("a", server1),
                ("b", server2)
            ],
            enableGlobalObjectIdentification: enableGlobalObjectIdentification);
    }

    public static class InaccessibleNodeField
    {
        public static class SourceSchema1
        {
            public class Query
            {
                public IEnumerable<Review> GetReviews()
                    => Review.All;
            }

            [Node]
            public record Review(int Id, string Body)
            {
                internal static readonly Review[] All =
                [
                    new Review(1, "A great read"),
                    new Review(2, "Could not put it down")
                ];

                public static Review? GetReview(int id)
                    => All.FirstOrDefault(r => r.Id == id);
            }
        }

        public static class SourceSchema2
        {
            public class Query
            {
                public string GetPublisher()
                    => "ChilliCream";
            }

            [Node]
            public record Review(int Id, int Stars)
            {
                internal static readonly Review[] All =
                [
                    new Review(1, 5),
                    new Review(2, 4)
                ];

                public static Review? GetReview(int id)
                    => All.FirstOrDefault(r => r.Id == id);
            }
        }
    }

    public static class InaccessibleField
    {
        public static class SourceSchema1
        {
            public record Book(int Id, string Title, Author Author);

            [EntityKey("id")]
            public record Author([property: Inaccessible] int Id);

            public class Query
            {
                private readonly OrderedDictionary<int, Book> _books =
                    new()
                    {
                        [1] = new Book(1, "C# in Depth", new Author(1)),
                        [2] = new Book(2, "The Lord of the Rings", new Author(2)),
                        [3] = new Book(3, "The Hobbit", new Author(2)),
                        [4] = new Book(4, "The Silmarillion", new Author(2))
                    };

                [Lookup]
                public Book? GetBookById(int id)
                    => _books[id];

                public string GetInaccessibleText(
                    [Inaccessible]
                    string text = "This is the default!")
                    => text;

                public IEnumerable<Book> GetBooks()
                    => _books.Values;
            }
        }

        public static class SourceSchema2
        {
            public record Author(int Id, string Name);

            public class Query
            {
                private readonly OrderedDictionary<int, Author> _authors = new()
                {
                    [1] = new Author(1, "Jon Skeet"),
                    [2] = new Author(2, "JRR Tolkien")
                };

                [Internal]
                [Lookup]
                public Author? GetAuthorById(int id)
                    => _authors[id];

                public SomeEnum GetEnumField(SomeEnum value)
                    => value;

                public string GetShareable(SomeInput input)
                    => $"A: {input.A}, B: {input.B}";
            }

            public enum SomeEnum
            {
                [Inaccessible] Item1,
                Item2
            }

            public record SomeInput(
                string A,
                [property: Inaccessible]
                [property: DefaultValue("ABC")]
                string B);
        }
    }

    public static class InaccessibleTypes
    {
        public static class SourceSchema1
        {
            [Inaccessible]
            public record InternalMetadata(string Version, string BuildNumber);

            // TODO: Re-add interface type once properly configured
            // [Inaccessible]
            // public interface InternalNode
            // {
            //     int InternalId { get; }
            // }
            //
            // // Implementation of InternalNode (also inaccessible so users can't query it)
            // [Inaccessible]
            // public record InternalNodeImpl(int InternalId) : InternalNode;

            // TODO: Re-add union type once properly configured
            // public record SuccessResult(string Message);
            //
            // public record ErrorResult(string Error);
            //
            // [Inaccessible]
            // [UnionType]
            // public abstract record InternalResult
            // {
            //     public sealed record Success(string Message) : InternalResult;
            //     public sealed record Error(string ErrorMessage) : InternalResult;
            // }

            [Inaccessible]
            public enum InternalStatus
            {
                Active,
                Inactive,
                Pending
            }

            [Inaccessible]
            public record InternalFilter(bool IncludeInternal);

            [Inaccessible]
            public class InternalScalarType : ScalarType
            {
                public InternalScalarType() : base("InternalScalar")
                {
                }

                public override Type RuntimeType
                    => typeof(string);

                public override ScalarSerializationType SerializationType
                    => ScalarSerializationType.String;

                protected override bool ApplySerializeAsToScalars => false;

                public override bool IsValueCompatible(IValueNode valueLiteral)
                    => valueLiteral is StringValueNode;

                public override object CoerceInputLiteral(IValueNode valueSyntax)
                    => ((StringValueNode)valueSyntax).Value;

                public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
                    => inputValue.GetString()!;

                public override void CoerceOutputValue(object runtimeValue, ResultElement resultValue)
                    => resultValue.SetStringValue(runtimeValue.ToString());

                public override IValueNode ValueToLiteral(object runtimeValue)
                    => new StringValueNode((string)runtimeValue);
            }

            public class Query
            {
                [Inaccessible]
                public InternalMetadata GetMetadata()
                    => new("1.0.0", "12345");

                // TODO: Re-add once interface type is properly configured
                // public InternalNode? GetInternalNode()
                //     => new InternalNodeImpl(123);

                // TODO: Re-add once union type is properly configured
                // public InternalResult GetInternalResult()
                //     => new InternalResult.Success("OK");

                public string GetStatusField([Inaccessible] InternalStatus status = InternalStatus.Active)
                    => status.ToString();

                public string GetFilterData([Inaccessible] InternalFilter? filter)
                    => $"IncludeInternal: {filter?.IncludeInternal}";
            }
        }

        public static class SourceSchema2
        {
            public class Query
            {
                public string GetDummy() => "dummy";
            }
        }
    }
}
