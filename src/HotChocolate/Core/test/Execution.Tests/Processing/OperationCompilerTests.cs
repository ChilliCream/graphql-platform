using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Processing
{
    public class OperationCompilerTests
    {
        [Fact]
        public void Compile_OperationId_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            // act
            void Action() =>
                OperationCompiler.Compile(
                    null!,
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Compile_Document_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            // act
            void Action() =>
                OperationCompiler.Compile(
                    "a",
                    null!,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Compile_Operation_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo }");

            // act
            void Action() =>
                OperationCompiler.Compile(
                    "a",
                    document,
                    null!,
                    schema,
                    schema.QueryType);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Compile_Schema_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            // act
            void Action() =>
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    null!,
                    schema.QueryType);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Compile_OperationType_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            // act
            void Action() =>
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Prepare_One_Field()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            // act
            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            Assert.Collection(
                operation.SelectionVariants,
                selectionSet =>
                {
                    Assert.Equal(operationDefinition.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.GetSelectionSet(schema.QueryType).Selections,
                        selection => Assert.Equal("foo", selection.ResponseName));
                });
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
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo foo }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            // act
            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            Assert.Collection(
                operation.SelectionVariants,
                selectionSet =>
                {
                    Assert.Equal(operationDefinition.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.GetSelectionSet(schema.QueryType).Selections,
                        selection => Assert.Equal("foo", selection.ResponseName));
                });
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
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            // act
            void Action() =>
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            Assert.Throws<GraphQLException>(Action);
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
            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            ISelection hero = operation.GetRootSelectionSet().Selections.Single();
            Assert.Equal("hero", hero.ResponseName);

            Assert.Collection(
                operation.GetSelectionSet(hero.SelectionSet!, schema.GetType<ObjectType>("Droid"))
                    .Selections,
                selection => Assert.Equal("name", selection.ResponseName),
                selection => Assert.Equal("primaryFunction", selection.ResponseName));

            Assert.Collection(
                operation.GetSelectionSet(hero.SelectionSet, schema.GetType<ObjectType>("Human"))
                    .Selections,
                selection => Assert.Equal("name", selection.ResponseName),
                selection => Assert.Equal("homePlanet", selection.ResponseName));

            operation.Print().MatchSnapshot();
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
            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            operation.Print().MatchSnapshot();
        }

        [Fact]
        public void Prepare_Duplicate_Field_With_Skip()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo @skip(if: true) foo @skip(if: false) }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            // act
            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            Assert.Collection(
                operation.SelectionVariants,
                selectionSet =>
                {
                    Assert.Equal(operationDefinition.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.GetSelectionSet(schema.QueryType).Selections,
                        selection => Assert.Equal("foo", selection.ResponseName));
                });
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
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo bar }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            // act
            void Action() =>
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            ISelectionSet rootSelections =
                operation.RootSelectionVariants.GetSelectionSet(
                    operation.RootSelectionVariants.GetPossibleTypes().First());
            ISelectionSet droidSelections =
                operation.GetSelectionSet(rootSelections.Selections[0].SelectionSet!, droid);

            Assert.Equal("name", droidSelections.Selections[0].ResponseName);
            Assert.False(droidSelections.Selections[0].IsConditional);
            Assert.True(droidSelections.Selections[0].IsIncluded(variables.Object));
            Assert.False(droidSelections.IsConditional);
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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            ISelectionSet rootSelections =
                operation.RootSelectionVariants.GetSelectionSet(
                    operation.RootSelectionVariants.GetPossibleTypes().First());
            ISelectionSet droidSelections =
                operation.GetSelectionSet(rootSelections.Selections[0].SelectionSet!, droid);

            Assert.Equal("name", droidSelections.Selections[0].ResponseName);
            Assert.False(droidSelections.Selections[0].IsConditional);
            Assert.True(droidSelections.Selections[0].IsIncluded(variables.Object));
            Assert.False(droidSelections.IsConditional);
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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            ISelectionSet rootSelections =
                operation.RootSelectionVariants.GetSelectionSet(
                    operation.RootSelectionVariants.GetPossibleTypes().First());
            ISelectionSet droidSelections =
                operation.GetSelectionSet(rootSelections.Selections[0].SelectionSet!, droid);

            Assert.Equal("name", droidSelections.Selections[0].ResponseName);
            Assert.True(droidSelections.Selections[0].IsConditional);
            Assert.True(droidSelections.Selections[0].IsIncluded(variables.Object));
            Assert.True(droidSelections.IsConditional);
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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            ISelectionSet rootSelections =
                operation.RootSelectionVariants.GetSelectionSet(
                    operation.RootSelectionVariants.GetPossibleTypes().First());
            ISelectionSet droidSelections =
                operation.GetSelectionSet(rootSelections.Selections[0].SelectionSet!, droid);

            Assert.Equal("name", droidSelections.Selections[0].ResponseName);
            Assert.False(droidSelections.Selections[0].IsConditional);
            Assert.True(droidSelections.Selections[0].IsIncluded(variables.Object));
            Assert.False(droidSelections.IsConditional);
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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            ISelectionSet rootSelections =
                operation.RootSelectionVariants.GetSelectionSet(
                    operation.RootSelectionVariants.GetPossibleTypes().First());
            ISelectionSet droidSelections =
                operation.GetSelectionSet(rootSelections.Selections[0].SelectionSet!, droid);

            Assert.Collection(
                droidSelections.Selections.Where(t => t.IsIncluded(vFalse.Object)),
                t => Assert.Equal("id", t.ResponseName));

            Assert.Collection(
                droidSelections.Selections.Where(t => t.IsIncluded(vTrue.Object)),
                t => Assert.Equal("name", t.ResponseName),
                t => Assert.Equal("id", t.ResponseName),
                t => Assert.Equal("height", t.ResponseName));

            operation.Print().MatchSnapshot();
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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            operation.Print().MatchSnapshot();
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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            ISelectionSet rootSelections =
                operation.RootSelectionVariants.GetSelectionSet(
                    operation.RootSelectionVariants.GetPossibleTypes().First());
            ISelectionSet droidSelections =
                operation.GetSelectionSet(rootSelections.Selections[0].SelectionSet!, droid);

            Assert.Collection(
                droidSelections.Selections.Where(t => t.IsIncluded(variables.Object)),
                t => Assert.Equal("id", t.ResponseName),
                t => Assert.Equal("height", t.ResponseName));

            operation.Print().MatchSnapshot();
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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            ISelectionSet rootSelections =
                operation.RootSelectionVariants.GetSelectionSet(
                    operation.RootSelectionVariants.GetPossibleTypes().First());
            ISelectionSet droidSelections =
                operation.GetSelectionSet(rootSelections.Selections[0].SelectionSet!, droid);

            Assert.Empty(droidSelections.Selections.Where(t => t.IsIncluded(variables.Object)));

            operation.Print().MatchSnapshot();
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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType,
                    optimizers);

            // assert
            operation.Print().MatchSnapshot();
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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            ISelection rootField = operation.GetRootSelectionSet().Selections.Single();

            ObjectType droid = schema.GetType<ObjectType>("Droid");

            Assert.Collection(
                operation.GetSelectionSet(rootField.SelectionSet!, droid).Fragments,
                f =>
                {
                    Assert.Equal(SyntaxKind.InlineFragment, f.SyntaxNode.Kind);
                    Assert.Collection(
                        f.SelectionSet.Selections,
                        s => Assert.Equal("id", s.ResponseName));
                });

            ObjectType human = schema.GetType<ObjectType>("Human");

            Assert.Collection(
                operation.GetSelectionSet(rootField.SelectionSet, human).Fragments,
                f =>
                {
                    Assert.Equal(SyntaxKind.InlineFragment, f.SyntaxNode.Kind);
                    Assert.Collection(
                        f.SelectionSet.Selections,
                        s => Assert.Equal("id", s.ResponseName));
                });
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
            var operation =
                (Operation)OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            ISelection rootField = operation.GetRootSelectionSet().Selections.Single();

            ObjectType droid = schema.GetType<ObjectType>("Droid");

            Assert.Collection(
                operation.GetSelectionSet(rootField.SelectionSet!, droid).Fragments,
                f =>
                {
                    Assert.Equal(SyntaxKind.FragmentDefinition, f.SyntaxNode.Kind);
                    Assert.Collection(
                        f.SelectionSet.Selections,
                        s => Assert.Equal("id", s.ResponseName));
                });

            ObjectType human = schema.GetType<ObjectType>("Human");

            Assert.Empty(operation.GetSelectionSet(rootField.SelectionSet, human).Fragments);
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
            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            operation
                .Print()
                .MatchSnapshot();
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
            void Action() =>
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            Assert.Throws<GraphQLException>(Action);
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
            void Action() =>
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            Assert.Throws<GraphQLException>(Action);
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
            void Action() =>
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // assert
            Assert.Throws<GraphQLException>(Action);
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
                if (!context.Path.IsEmpty && context.Path.Peek() is { Name: { Value: "bar" } })
                {
                    IObjectField baz = context.Type.Fields["baz"];
                    FieldNode bazSelection = Utf8GraphQLParser.Syntax.ParseField("baz { text }");
                    FieldDelegate bazPipeline = context.CompileResolverPipeline(baz, bazSelection);

                    var compiledSelection = new Selection(
                        context.Type,
                        baz,
                        bazSelection,
                        bazPipeline,
                        internalSelection: true);

                    context.Fields[compiledSelection.ResponseName] = compiledSelection;
                }
            }
        }
    }
}
