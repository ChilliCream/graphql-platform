using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

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

        Assert.Collection(nodeInterface.Fields,
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
    public async Task Infer_Node_From_Query_Field_Argument_Decodes_Ids()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("{ fooById(id: \"Rm9vCmRhYmM=\") { id clearTextId } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_Resolve_Node()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("{ node(id: \"Rm9vCmRhYmM=\") { id __typename } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_With_Abc_Argument_Resolve_Node()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query2>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("{ node(id: \"Rm9vCmRhYmM=\") { id __typename } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_With_Abc_Argument_Resolve_Nodes_Single()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query2>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync("{ nodes(ids: \"Rm9vCmRhYmM=\") { id __typename } }")
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
                "{ nodes(ids: [\"Rm9vCmRhYmM=\", \"Rm9vCmRhYmM=\"]) { id __typename } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_Field_Resolve_Node_With_Int_Id()
    {
        var serializer = new IdSerializer();
        var id = serializer.Serialize("Bar", 123);

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query3>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query ($id: ID!) {
                            node(id: $id) {
                                id 
                                __typename 
                                ... on Bar {
                                    clearTextId
                                }
                            } 
                        }")
                    .SetVariableValue("id", id)
                    .Create())
            .MatchSnapshotAsync();
    }

    public class Query
    {
        [NodeResolver]
        public Foo GetFooById(string id)
            => new Foo(id);
    }

    public class Query2
    {
        [NodeResolver]
        public Foo GetFooById(string abc)
            => new Foo(abc);
    }

    public class Query3
    {
        [NodeResolver]
        public Bar GetBarById(int id)
            => new Bar(id);
    }

    public class Foo
    {
        public Foo(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string ClearTextId => Id;
    }

    public class Bar
    {
        public Bar(int id)
        {
            Id = id;
        }

        public int Id { get; }

        public int ClearTextId => Id;
    }
}
