using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

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
        var serializer = new IdSerializer();
        var id = serializer.Serialize("Foo1", 123);

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query8>()
            .AddTypeExtension<Foo2>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"query ($id: ID!) {
                            node(id: $id) {
                                id
                                __typename
                                ... on Foo1 {
                                    clearTextId
                                }
                            }
                        }")
                    .SetVariableValue("id", id)
                    .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeResolver_Is_Missing()
    {
        async Task Error() => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query9>()
            .ModifyOptions(o => o.EnsureAllNodesCanBeResolved = true)
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync();

        var error = await Assert.ThrowsAsync<SchemaException>(Error);

        error.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Infer_Node_From_Query_As_Interface_From_Interface()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query10>()
            .AddType<Query10.DrivingLicense>() // TODO: Should this be discovered?
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_As_Interface_From_Abstract()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query11>()
            .AddType<Query11.DrivingLicense>() // TODO: Should this be discovered?
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Infer_Node_From_Query_As_Interface_From_Abstract_With_Explicit_Types()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query12>()
            .AddType<Query12.DocumentType>()
            .AddType<Query12.DrivingLicenseType>()
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync()
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
        public Baz GetBarById(int id)
            => new Baz(id);
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

    public class Baz
    {
        public Baz(int id)
        {
            Id1 = id;
        }

        public int Id1 { get; }

        public int ClearTextId => Id1;
    }

    public class Query7
    {
        [NodeResolver]
        public Qux GetBarById(string id)
            => new Qux(id);
    }

    [Node]
    public class Qux
    {
        public Qux(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string ClearTextId => Id;
    }

    public class Query8
    {
        [NodeResolver]
        public Foo1 GetBarById(string id)
            => new Foo1(id);
    }

    public class Foo1
    {
        public Foo1(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string ClearTextId => Id;
    }

    [Node]
    [ExtendObjectType(typeof(Foo1))]
    public class Foo2
    {

    }

    public class Query9
    {
        public Qux GetBarById(string id)
            => new Qux(id);
    }

    public class Query10
    {
        [NodeResolver]
        public IDocument GetDocumentById(string id)
            => new DrivingLicense(id);

        public interface IDocument
        {
            public string Id { get; }
        }

        [Node]
        public record DrivingLicense(string Id) : IDocument;
    }

    public class Query11
    {
        [NodeResolver]
        public Document GetDocumentById(string id)
            => new DrivingLicense(id);

        // TODO: Will be nice to have it as interface in schema
        [Node]
        public abstract record Document(string Id);

        public record DrivingLicense(string Id) : Document(Id);
    }

    public class Query12
    {
        [NodeResolver]
        public Document GetDocumentById(string id)
            => new DrivingLicense(id);

        public abstract record Document(string Id);

        public class DocumentType : InterfaceType<Document>
        {
            protected override void Configure(
                IInterfaceTypeDescriptor<Document> descriptor) { }
        }

        public record DrivingLicense(string Id) : Document(Id);

        public class DrivingLicenseType : ObjectType<DrivingLicense>
        {
            protected override void Configure(
                IObjectTypeDescriptor<DrivingLicense> descriptor)
            {
                descriptor.ImplementsNode();
                descriptor.Implements<DocumentType>();
            }
        }
    }
}
