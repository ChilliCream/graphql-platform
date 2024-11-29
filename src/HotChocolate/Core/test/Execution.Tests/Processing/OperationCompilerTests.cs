using System.Text;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Execution.Processing;

public class OperationCompilerTests
{
    [Fact]
    public void Prepare_One_Field()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo"))
            .Create();

        var document = Utf8GraphQLParser.Parse("{ foo }");

        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Prepare_Duplicate_Field()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo"))
            .Create();

        var document = Utf8GraphQLParser.Parse("{ foo foo }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Prepare_Empty_Operation_SelectionSet()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo"))
            .Create();

        var document = Utf8GraphQLParser.Parse("{ }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Prepare_Inline_Fragment()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
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

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Prepare_Fragment_Definition()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
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

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Nested_Fragments_with_Conditions()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query ($if: Boolean!) {
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
            fragment FriendEdge1 on FriendsEdge {
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
                name
                otherHuman {
                  __typename
                  name
                }
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Prepare_Duplicate_Field_With_Skip()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo"))
            .Create();

        var document = Utf8GraphQLParser.Parse(
            "{ foo @skip(if: true) foo @skip(if: false) }");

        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Field_Does_Not_Exist()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("foo"))
            .Create();

        var document = Utf8GraphQLParser.Parse("{ foo bar }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        void Action()
        {
            var compiler = new OperationCompiler(new InputParser());
            compiler.Compile(
                new OperationCompilerRequest(
                    "opid",
                    document,
                    operationDefinition,
                    schema.QueryType,
                    schema));
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
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(false);

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean!){
                hero(episode: EMPIRE) {
                    name
                    ... abc @include(if: $v)
                }
            }

            fragment abc on Droid {
                name
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Field_Is_Visible_When_One_Selection_Is_Visible_2()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(false);

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean!){
                hero(episode: EMPIRE) {
                    name @include(if: $v)
                    ... abc
                }
            }

            fragment abc on Droid {
                name
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Field_Is_Visible_When_One_Selection_Is_Visible_3()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>()))
            .Returns((string name) => name.EqualsOrdinal("q"));

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean!, $q: Boolean!){
                hero(episode: EMPIRE) {
                    name @include(if: $v)
                    ... abc @include(if: $q)
                }
            }

            fragment abc on Droid {
                name
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Field_Is_Visible_When_One_Selection_Is_Visible_4()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(false);

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean!){
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

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Object_Field_Visibility_Is_Correctly_Inherited()
    {
        // arrange
        var vFalse = new Mock<IVariableValueCollection>();
        vFalse.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(false);

        var vTrue = new Mock<IVariableValueCollection>();
        vTrue.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(true);

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean!) {
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

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Nested_Fragments()
    {
        // arrange
        var vFalse = new Mock<IVariableValueCollection>();
        vFalse.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(false);

        var vTrue = new Mock<IVariableValueCollection>();
        vTrue.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(true);

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query ($if: Boolean!) {
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
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Object_Field_Visibility_Is_Correctly_Inherited_2()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>()))
            .Returns((string name) => name.EqualsOrdinal("v"));

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean!, $q: Boolean!) {
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

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Object_Field_Visibility_Is_Correctly_Inherited_3()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>()))
            .Returns((string name) => name.EqualsOrdinal("v"));

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean!, $q: Boolean!) {
                hero(episode: EMPIRE) @include(if: $v) {
                    name @include(if: $q)
                }
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Field_Based_Optimizers()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>()))
            .Returns((string name) => name.EqualsOrdinal("v"));

        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("root")
                    .Resolve(new Foo())
                    .UseOptimizer(new SimpleOptimizer()))
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"{
                root {
                    bar {
                        text
                    }
                }
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Defer_Inline_Fragment()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"{
                hero(episode: EMPIRE) {
                    name
                    ... @defer {
                        id
                    }
                }
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Defer_Fragment_Spread()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"{
                hero(episode: EMPIRE) {
                    name
                    ... Foo @defer
                }
            }

            fragment Foo on Droid {
                id
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void Reuse_Selection()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
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

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void FragmentSpread_SelectionsSet_Empty()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(false);

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean){
                hero(episode: EMPIRE) {
                    name @include(if: $v)
                    ... abc
                }
            }

            fragment abc on Droid { }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void InlineFragment_SelectionsSet_Empty()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(false);

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean){
                hero(episode: EMPIRE) {
                    name @include(if: $v)
                    ... on Droid { }
                }
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public void CompositeType_SelectionsSet_Empty()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(false);

        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            @"query foo($v: Boolean) {
                hero(episode: EMPIRE) { }
            }");

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Large_Query_Test()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .BuildSchemaAsync();

        var document = Utf8GraphQLParser.Parse(
            FileResource.Open("LargeQuery.graphql"));

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Crypto_Details_Test()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("Crypto.graphql"))
                .UseField(next => next)
                .BuildSchemaAsync();

        var document = Utf8GraphQLParser.Parse(
            FileResource.Open("CryptoDetailQuery.graphql"));

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Crypto_Include()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("Crypto.graphql"))
                .UseField(next => next)
                .BuildSchemaAsync();

        var document = Utf8GraphQLParser.Parse(
            """
            query Crypto {
                assetBySymbol(symbol: "BTC") {
                    price {
                        lastPrice
                    }
                    ... PriceInfo @include(if: false)
                }
            }

            fragment PriceInfo on Asset {
              price {
                lastPrice
              }
            }
            """);

        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Crypto_Fragment_Removes_Conditional_State_For_Price()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("Crypto.graphql"))
                .UseField(next => next)
                .BuildSchemaAsync();

        var document = Utf8GraphQLParser.Parse(
            """
            query Crypto {
                assetBySymbol(symbol: "BTC") {
                    price @include(if: false) {
                        lastPrice
                    }
                    ... PriceInfo
                }
            }

            fragment PriceInfo on Asset {
              price {
                lastPrice
              }
            }
            """);

        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Crypto_Conditional_Fragment_Has_Additional_Field()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("Crypto.graphql"))
                .UseField(next => next)
                .BuildSchemaAsync();

        var document = Utf8GraphQLParser.Parse(
            """
            query Crypto {
                assetBySymbol(symbol: "BTC") {
                    price {
                        lastPrice
                    }
                    ... PriceInfo @include(if: false)
                }
            }

            fragment PriceInfo on Asset {
              price {
                lastPrice
                currency # this field should be conditional
              }
            }
            """);

        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Crypto_List_Test()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("Crypto.graphql"))
                .UseField(next => next)
                .BuildSchemaAsync();

        var document = Utf8GraphQLParser.Parse(
            FileResource.Open("CryptoQuery.graphql"));

        var operationDefinition =
            document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task AbstractTypes()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(
                    """
                    type Query {
                      organizationUnits: [OrganizationUnit!]!
                    }

                    interface OrganizationUnit {
                      id: ID!
                      name: String!
                      someType: SomeType!
                      children: [OrganizationUnit!]!
                    }

                    type SomeType {
                        id: ID!
                        name: String!
                    }

                    type OrganizationUnit1 implements OrganizationUnit {
                      id: ID!
                      name: String!
                      someType: SomeType!
                      children: [OrganizationUnit!]!
                    }

                    type OrganizationUnit2 implements OrganizationUnit {
                      id: ID!
                      name: String!
                      someType: SomeType!
                      children: [OrganizationUnit!]!
                    }
                    """)
                .UseField(next => next)
                .BuildSchemaAsync();

        var document = Utf8GraphQLParser.Parse(
            """
            {
                organizationUnits {
                    id
                    name
                    someType {
                        id
                        name
                    }
                    children {
                        id
                        name
                        someType {
                            id
                            name
                        }
                        children {
                            id
                            name
                            someType {
                                id
                                name
                            }
                        }
                    }
                }
            }
            """);

        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Ensure_Selection_Backlog_Does_Not_Exponentially_Grow()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(
                    """
                    type Query {
                      organizationUnits: [OrganizationUnit!]!
                    }

                    interface OrganizationUnit {
                      id: ID!
                      name: String!
                      someType: SomeType!
                      children: [OrganizationUnit!]!
                    }

                    type SomeType {
                        id: ID!
                        name: String!
                    }

                    type OrganizationUnit1 implements OrganizationUnit {
                      id: ID!
                      name: String!
                      someType: SomeType!
                      children: [OrganizationUnit!]!
                    }

                    type OrganizationUnit2 implements OrganizationUnit {
                      id: ID!
                      name: String!
                      someType: SomeType!
                      children: [OrganizationUnit!]!
                    }
                    """)
                .UseField(next => next)
                .BuildSchemaAsync();

        var document = Utf8GraphQLParser.Parse(
            """
            {
                organizationUnits {
                    id
                    name
                    someType {
                        id
                        name
                    }
                    children {
                        id
                        name
                        someType {
                            id
                            name
                        }
                        children {
                            id
                            name
                            someType {
                                id
                                name
                            }
                        }
                    }
                }
            }
            """);

        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        Assert.Equal(29, compiler.Metrics.Selections);
        Assert.Equal(7, compiler.Metrics.SelectionSetVariants);
        Assert.Equal(4, compiler.Metrics.BacklogMaxSize);
    }

    [Fact]
    public async Task Resolve_Concrete_Types_From_Unions()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<UnionQuery>()
                .AddType<TypeOne>()
                .AddType<TypeTwo>()
                .UseField(next => next)
                .BuildSchemaAsync();

        var document = Utf8GraphQLParser.Parse(
            """
            query QueryName {
              oneOrTwo {
                ...TypeOneParts
                ...TypeTwoParts
              }
            }

            fragment TypeOneParts on TypeOne {
              field1 { name }
            }

            fragment TypeTwoParts on TypeTwo {
              field1 { name }
            }
            """);

        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().Single();

        // act
        var compiler = new OperationCompiler(new InputParser());
        var operation = compiler.Compile(
            new OperationCompilerRequest(
                "opid",
                document,
                operationDefinition,
                schema.QueryType,
                schema));

        // assert
        MatchSnapshot(document, operation);
    }

    [Fact]
    public async Task Resolve_Concrete_Types_From_Unions_Execute()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<UnionQuery>()
                .AddType<TypeOne>()
                .AddType<TypeTwo>()
                .BuildRequestExecutorAsync();

        var document = Utf8GraphQLParser.Parse(
            """
            query QueryName {
              oneOrTwo {
                ...TypeOneParts
                ...TypeTwoParts
              }
            }

            fragment TypeOneParts on TypeOne {
              field1 { name }
            }

            fragment TypeTwoParts on TypeTwo {
              field1 { name }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(builder => builder.SetDocument(document));

        // assert
        result.MatchSnapshot();
    }

    private static void MatchSnapshot(DocumentNode original, IOperation compiled)
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
        public Bar Bar => new();
    }

    public class Bar
    {
        public string Text => "Bar";

        public Baz Baz => new();
    }

    public class Baz
    {
        public string Text => "Baz";
    }

    public class SimpleOptimizer : ISelectionSetOptimizer
    {
        public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
        {
            if (context.Path is { Name: "bar", })
            {
                var baz = context.Type.Fields["baz"];
                var bazSelection = Utf8GraphQLParser.Syntax.ParseField("baz { text }");
                var bazPipeline = context.CompileResolverPipeline(baz, bazSelection);

                var compiledSelection = new Selection(
                    context.GetNextSelectionId(),
                    context.Type,
                    baz,
                    baz.Type,
                    bazSelection,
                    "someName",
                    resolverPipeline: bazPipeline,
                    isInternal: true);

                context.AddSelection(compiledSelection);
            }
        }
    }

    public class UnionQuery
    {
        public IOneOrTwo OneOrTwo() => new TypeOne();
    }

    public class TypeOne : IOneOrTwo
    {
        public FieldOne1 Field1 => new();
    }

    public class TypeTwo : IOneOrTwo
    {
        public FieldTwo1 Field1 => new();
    }

    [UnionType]
    public interface IOneOrTwo;

    public class FieldOne1
    {
        public string Name => "Name";
    }

    public class FieldTwo1
    {
        public string Name => "Name";
    }

}
