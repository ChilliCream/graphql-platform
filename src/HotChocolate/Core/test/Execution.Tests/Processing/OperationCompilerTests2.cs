using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Processing;

public class OperationCompilerTests2
{
    [Fact]
    public void Prepare_One_Field()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("foo"))
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse("{ foo }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Prepare_Duplicate_Field()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("foo"))
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse("{ foo foo }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Prepare_Empty_Operation_SelectionSet()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("foo"))
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse("{ }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Prepare_Inline_Fragment()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"{
                hero(episode: EMPIRE) {
                    name
                    ... on Droid {
                        primaryFunction
                    }
                    ... on Human {
                        homePlanet
                    }
                }
             }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Prepare_Fragment_Definition()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"{
                hero(episode: EMPIRE) {
                    name
                    ... abc
                    ... def
                }
              }

              fragment abc on Droid {
                  primaryFunction
              }

              fragment def on Human {
                  homePlanet
              }
             ");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Prepare_Duplicate_Field_With_Skip()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("foo"))
            .Create();

        var document = Utf8GraphQLParser.Parse(
            "{ foo @skip(if: true) foo @skip(if: false) }");

        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Field_Does_Not_Exist()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("foo"))
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse("{ foo bar }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        void Action()
        {
            var compiler = new OperationCompiler2(new InputParser());
            compiler.Compile("opid", operationDefinition, schema.QueryType, document, schema);
        }

        // assert
        Assert.Equal(
            "Field `bar` does not exist on type `Query`.",
            Assert.Throws<GraphQLException>(Action).Message);
    }

    [Fact]
    public void Field_Is_Visible_When_One_Selection_Is_Visible_1()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(false);

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        ObjectType droid = schema.GetType<ObjectType>("Droid");

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean){
                    hero(episode: EMPIRE) {
                        name
                        ... abc @include(if: $v)
                    }
                }

                fragment abc on Droid {
                    name
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Field_Is_Visible_When_One_Selection_Is_Visible_2()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(false);

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        ObjectType droid = schema.GetType<ObjectType>("Droid");

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean){
                    hero(episode: EMPIRE) {
                        name @include(if: $v)
                        ... abc
                    }
                }

                fragment abc on Droid {
                    name
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Field_Is_Visible_When_One_Selection_Is_Visible_3()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>()))
            .Returns((NameString name) => name.Equals("q"));

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        ObjectType droid = schema.GetType<ObjectType>("Droid");

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean, $q: Boolean){
                    hero(episode: EMPIRE) {
                        name @include(if: $v)
                        ... abc @include(if: $q)
                    }
                }

                fragment abc on Droid {
                    name
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Field_Is_Visible_When_One_Selection_Is_Visible_4()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(false);

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        ObjectType droid = schema.GetType<ObjectType>("Droid");

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean){
                    hero(episode: EMPIRE) {
                        name @include(if: $v)
                        ... abc
                    }
                }

                fragment abc on Droid {
                    name
                    ... {
                        name
                    }
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Object_Field_Visibility_Is_Correctly_Inherited()
    {
        // arrange
        var vFalse = new Mock<IVariableValueCollection>();
        vFalse.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(false);

        var vTrue = new Mock<IVariableValueCollection>();
        vTrue.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(true);

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        ObjectType droid = schema.GetType<ObjectType>("Droid");

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean) {
                    hero(episode: EMPIRE) @include(if: $v) {
                        name
                    }
                    ... on Query {
                        hero(episode: EMPIRE) {
                            id
                        }
                    }
                    ... @include(if: $v) {
                        hero(episode: EMPIRE) {
                            height
                        }
                    }
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Nested_Fragments()
    {
        // arrange
        var vFalse = new Mock<IVariableValueCollection>();
        vFalse.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(false);

        var vTrue = new Mock<IVariableValueCollection>();
        vTrue.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(true);

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"
                query ($if: Boolean!) {
                    human(id: ""1000"") {
                        ... Human1 @include(if: $if)
                        ... Human2 @skip(if: $if)
                    }
                }
                fragment Human1 on Human {
                    friends {
                        edges {
                            ... FriendEdge1
                        }
                    }
                }
                fragment FriendEdge1 on CharacterEdge {
                    node {
                        __typename
                        friends {
                            nodes {
                                __typename
                                ... Human3
                            }
                        }
                    }
                }
                fragment Human2 on Human {
                    friends {
                        edges {
                            node {
                                __typename
                                ... Human3
                            }
                        }
                    }
                }
                fragment Human3 on Human {
                    # This works
                    name

                    # This is returned as an empty object but should be populated
                    otherHuman {
                      __typename
                      name
                    }
                }
                ");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Object_Field_Visibility_Is_Correctly_Inherited_2()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>()))
            .Returns((NameString name) => name.Equals("v"));

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        ObjectType droid = schema.GetType<ObjectType>("Droid");

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean, $q: Boolean) {
                    hero(episode: EMPIRE) @include(if: $v) {
                        name @include(if: $q)
                    }
                    ... on Query {
                        hero(episode: EMPIRE) {
                            id
                        }
                    }
                    ... @include(if: $v) {
                        hero(episode: EMPIRE) {
                            height
                        }
                    }
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Object_Field_Visibility_Is_Correctly_Inherited_3()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>()))
            .Returns((NameString name) => name.Equals("v"));

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        ObjectType droid = schema.GetType<ObjectType>("Droid");

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean, $q: Boolean) {
                    hero(episode: EMPIRE) @include(if: $v) {
                        name @include(if: $q)
                    }
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Field_Based_Optimizers()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>()))
            .Returns((NameString name) => name.Equals("v"));

        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("root")
                .Resolve(new Foo())
                .UseOptimizer(new SimpleOptimizer()))
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"{
                    root {
                        bar {
                            text
                        }
                    }
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        var optimizers = new List<NoopOptimizer> { new NoopOptimizer() };

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Defer_Inline_Fragment()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"{
                    hero(episode: EMPIRE) {
                        name
                        ... @defer {
                            id
                        }
                    }
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Defer_Fragment_Spread()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"{
                    hero(episode: EMPIRE) {
                        name
                        ... Foo @defer
                    }
                }

                fragment Foo on Droid {
                    id
                }
                ");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Reuse_Selection()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query Hero($episode: Episode, $withFriends: Boolean!) {
                    hero(episode: $episode) {
                        name
                        friends @include(if: $withFriends) {
                            nodes {
                                id
                            }
                        }
                    }
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void FragmentSpread_SelectionsSet_Empty()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(false);

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean){
                    hero(episode: EMPIRE) {
                        name @include(if: $v)
                        ... abc
                    }
                }

                fragment abc on Droid { }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void InlineFragment_SelectionsSet_Empty()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(false);

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean){
                    hero(episode: EMPIRE) {
                        name @include(if: $v)
                        ... on Droid { }
                    }
                }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void CompositeType_SelectionsSet_Empty()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(false);

        ISchema schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean) {
                hero(episode: EMPIRE) { }
            }");

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Large_Query_Test()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();

        ISchema schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .BuildSchemaAsync();

        DocumentNode document = Utf8GraphQLParser.Parse(
            FileResource.Open("LargeQuery.graphql"));

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Crypto_Details_Test()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();

        ISchema schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("Crypto.graphql"))
                .UseField(next => next)
                .BuildSchemaAsync();

        DocumentNode document = Utf8GraphQLParser.Parse(
            FileResource.Open("CryptoDetailQuery.graphql"));

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Crypto_List_Test()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();

        ISchema schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("Crypto.graphql"))
                .UseField(next => next)
                .BuildSchemaAsync();

        DocumentNode document = Utf8GraphQLParser.Parse(
            FileResource.Open("CryptoQuery.graphql"));

        OperationDefinitionNode operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler2(new InputParser());
        var operation = compiler.Compile(
            "opid",
            operationDefinition,
            schema.QueryType,
            document,
            schema);

        // assert
        MatchSnapshot(document, operation);
    }

    private static void MatchSnapshot(DocumentNode original, IPreparedOperation2 compiled)
    {
        var sb = new StringBuilder();
        sb.AppendLine(original.ToString());
        sb.AppendLine();
        sb.AppendLine("---------------------------------------------------------");
        sb.AppendLine();
        sb.AppendLine(compiled.ToString());
        sb.ToString().Replace("\r", "").MatchSnapshot();
    }

    public class Foo
    {
        public Bar Bar => new Bar();
    }

    public class Bar
    {
        public string Text => "Bar";

        public Baz Baz => new Baz();
    }

    public class Baz
    {
        public string Text => "Baz";
    }

    public class NoopOptimizer : ISelectionOptimizer
    {
        public bool AllowFragmentDeferral(
            SelectionOptimizerContext context,
            InlineFragmentNode fragment) => true;

        public bool AllowFragmentDeferral(
            SelectionOptimizerContext context,
            FragmentSpreadNode fragmentSpread,
            FragmentDefinitionNode fragmentDefinition) => true;

        public void OptimizeSelectionSet(SelectionOptimizerContext context)
        {
        }
    }

    public class SimpleOptimizer : ISelectionOptimizer
    {
        public bool AllowFragmentDeferral(
            SelectionOptimizerContext context,
            InlineFragmentNode fragment) => true;

        public bool AllowFragmentDeferral(
            SelectionOptimizerContext context,
            FragmentSpreadNode fragmentSpread,
            FragmentDefinitionNode fragmentDefinition) => true;

        public void OptimizeSelectionSet(SelectionOptimizerContext context)
        {
            if (!context.Path.IsEmpty && context.Path.Peek() is { Name.Value: "bar" })
            {
                IObjectField baz = context.Type.Fields["baz"];
                FieldNode bazSelection = Utf8GraphQLParser.Syntax.ParseField("baz { text }");
                FieldDelegate bazPipeline = context.CompileResolverPipeline(baz, bazSelection);

                var compiledSelection = new Selection(
                    context.GetNextId(),
                    context.Type,
                    baz,
                    baz.Type,
                    bazSelection,
                    bazPipeline,
                    internalSelection: true);

                context.Fields[compiledSelection.ResponseName] = compiledSelection;
            }
        }
    }
}
