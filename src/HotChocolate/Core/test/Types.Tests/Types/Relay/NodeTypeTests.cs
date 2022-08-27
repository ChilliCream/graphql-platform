using System.Diagnostics.Contracts;
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

    //

    public class Query
    {
        public Foo CreateFoo()
            => new Foo("abc");

        [NodeResolver]
        public Foo GetFooById(string id)
            => new Foo(id);
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
}
