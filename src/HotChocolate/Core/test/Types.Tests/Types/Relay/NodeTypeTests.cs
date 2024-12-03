using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class NodeTypeTests : TypeTestBase
{
    [Fact]
    public void InitializeExplicitFieldWithImplicitResolver()
    {
        // arrange
        // act
        var nodeInterface = CreateType(
            new NodeType(),
            b => b.ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.Equal(
            "Node",
            nodeInterface.Name);

        Assert.Equal(
            "The node interface is implemented by entities that have " +
            "a global unique identifier.",
            nodeInterface.Description);

        Assert.Collection(
            nodeInterface.Fields,
            t =>
            {
                Assert.Equal("id", t.Name);
                Assert.IsType<IdType>(
                    Assert.IsType<NonNullType>(t.Type).Type);
            });
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_OneArgRule_Violated()
    {
        async Task Error() => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query4>()
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync();

        var exception = await Assert.ThrowsAsync<SchemaException>(Error);

        exception.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_ResultNotAnObjectRule_Violated()
    {
        async Task Error() => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query5>()
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync();

        var exception = await Assert.ThrowsAsync<SchemaException>(Error);

        exception.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_NoIdOnResultRule_Violated()
    {
        async Task Error() => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query6>()
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync();

        var exception = await Assert.ThrowsAsync<SchemaException>(Error);

        exception.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_Argument_Decodes_Ids()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("{ fooById(id: \"Rm9vOmFiYw==\") { id clearTextId } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_Resolve_Node()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("{ node(id: \"Rm9vOmFiYw==\") { id __typename } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_With_Abc_Argument_Resolve_Node()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query2>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("{ node(id: \"Rm9vOmFiYw==\") { id __typename } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_With_Abc_Argument_Resolve_Nodes_Single()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query2>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("{ nodes(ids: \"Rm9vOmFiYw==\") { id __typename } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_With_Abc_Argument_Resolve_Nodes_Two()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query2>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync(
                "{ nodes(ids: [\"Rm9vOmFiYw==\", \"Rm9vOmFiYw==\"]) { id __typename } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_Resolve_Node_With_Int_Id()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query3>()
            .AddGlobalObjectIdentification()
            .BuildRequestExecutorAsync();

        var serializer = executor.Schema.Services.GetRequiredService<INodeIdSerializer>();
        var id = serializer.Format("Bar", 123);

        await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        @"query ($id: ID!) {
                            node(id: $id) {
                                id
                                __typename
                                ... on Bar {
                                    clearTextId
                                }
                            }
                        }")
                    .SetVariableValues(new Dictionary<string, object> { { "id", id }, })
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Node_Attribute_Does_Not_Throw_With_NodeResolver_On_Query()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query7>()
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync();

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Node_Attribute_Does_Not_Throw_With_NodeResolver_On_Query_2()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query8>()
            .AddTypeExtension<Foo2>()
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync();

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Node_Attribute_Does_Not_Throw_Execute_Query()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query8>()
            .AddTypeExtension<Foo2>()
            .AddGlobalObjectIdentification()
            .BuildRequestExecutorAsync();

        var serializer = executor.Schema.Services.GetRequiredService<INodeIdSerializer>();
        var id = serializer.Format("Foo1", "123");

        await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        @"query ($id: ID!) {
                            node(id: $id) {
                                id
                                __typename
                                ... on Foo1 {
                                    clearTextId
                                }
                            }
                        }")
                    .SetVariableValues(new Dictionary<string, object> { { "id", id }, })
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeResolver_Is_Missing()
    {
        async Task Error() => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query9>()
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync();

        var error = await Assert.ThrowsAsync<SchemaException>(Error);

        error.Message.MatchSnapshot();
    }

    [Fact]
    public async Task NodeResolver_Is_Missing_EnsureAllNodesCanBeResolved_False()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query9>()
            .ModifyOptions(o => o.EnsureAllNodesCanBeResolved = false)
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync();

        Assert.NotNull(schema);
    }

    public class Query
    {
        [NodeResolver]
        public Foo GetFooById(string id) => new(id);
    }

    public class Query2
    {
        [NodeResolver]
        public Foo GetFooById(string abc) => new(abc);
    }

    public class Query3
    {
        [NodeResolver]
        public Bar GetBarById(int id) => new(id);
    }

    public class Query4
    {
        [NodeResolver]
        public Bar GetBarById(int id1, int id2)
            => throw new InvalidCastException("Should never have come to this point.");
    }

    public class Query5
    {
        [NodeResolver]
        public int GetBarById(int id) => 0;
    }

    public class Query6
    {
        [NodeResolver]
        public Baz GetBarById(int id) => new(id);
    }

    public class Foo(string id)
    {
        public string Id { get; } = id;

        public string ClearTextId => Id;
    }

    public class Bar(int id)
    {
        public int Id { get; } = id;

        public int ClearTextId => Id;
    }

    public class Baz(int id)
    {
        public int Id1 { get; } = id;

        public int ClearTextId => Id1;
    }

    public class Query7
    {
        [NodeResolver]
        public Qux GetBarById(string id) => new(id);
    }

    [Node]
    public class Qux(string id)
    {
        public string Id { get; } = id;

        public string ClearTextId => Id;
    }

    public class Query8
    {
        [NodeResolver]
        public Foo1 GetBarById(string id) => new(id);
    }

    public class Foo1(string id)
    {
        public string Id { get; } = id;

        public string ClearTextId => Id;
    }

    [Node]
    [ExtendObjectType(typeof(Foo1))]
    public class Foo2;

    public class Query9
    {
        public Qux GetBarById(string id) => new(id);
    }
}
