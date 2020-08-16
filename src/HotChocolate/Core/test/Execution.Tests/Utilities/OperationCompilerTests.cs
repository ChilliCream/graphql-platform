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

namespace HotChocolate.Execution.Utilities
{
    public class OperationCompilerTests
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
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo }");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            Assert.Collection(
                selectionSets.Values,
                selectionSet =>
                {
                    Assert.Equal(operation.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.GetSelections(schema.QueryType),
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            Assert.Collection(
                selectionSets.Values,
                selectionSet =>
                {
                    Assert.Equal(operation.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.GetSelections(schema.QueryType),
                        selection => Assert.Equal("foo", selection.ResponseName));
                });
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            IPreparedSelection hero = selectionSets[operation.SelectionSet].GetSelections(schema.QueryType).Single();
            Assert.Equal("hero", hero.ResponseName);

            Assert.Collection(
                selectionSets[hero.SelectionSet].GetSelections(schema.GetType<ObjectType>("Droid")),
                selection => Assert.Equal("name", selection.ResponseName),
                selection => Assert.Equal("primaryFunction", selection.ResponseName));

            Assert.Collection(
                selectionSets[hero.SelectionSet].GetSelections(schema.GetType<ObjectType>("Human")),
                selection => Assert.Equal("name", selection.ResponseName),
                selection => Assert.Equal("homePlanet", selection.ResponseName));

            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            op.Print().MatchSnapshot();
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            op.Print().MatchSnapshot();
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            Assert.Collection(
                selectionSets.Values,
                selectionSet =>
                {
                    Assert.Equal(operation.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.GetSelections(schema.QueryType),
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            Action action = () =>
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            Assert.Equal(
                "Field `bar` does not exist on type `Query`.",
                Assert.Throws<GraphQLException>(action).Message);
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            IPreparedSelectionList rootSelections =
                op.RootSelectionSet.GetSelections(op.RootSelectionSet.GetPossibleTypes().First());
            IPreparedSelectionList droidSelections =
                op.GetSelections(rootSelections[0].SelectionSet!, droid);

            Assert.Equal("name", droidSelections[0].ResponseName);
            Assert.False(droidSelections[0].IsConditional);
            Assert.True(droidSelections[0].IsIncluded(variables.Object));
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            IPreparedSelectionList rootSelections =
                op.RootSelectionSet.GetSelections(op.RootSelectionSet.GetPossibleTypes().First());
            IPreparedSelectionList droidSelections =
                op.GetSelections(rootSelections[0].SelectionSet!, droid);

            Assert.Equal("name", droidSelections[0].ResponseName);
            Assert.False(droidSelections[0].IsConditional);
            Assert.True(droidSelections[0].IsIncluded(variables.Object));
            Assert.False(droidSelections.IsConditional);
        }

        [Fact]
        public void Field_Is_Visible_When_One_Selection_Is_Visible_3()
        {
            // arrange
            var variables = new Mock<IVariableValueCollection>();
            variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>()))
                .Returns((NameString name) =>
                {
                    return name.Equals("q");
                });

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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            IPreparedSelectionList rootSelections =
                op.RootSelectionSet.GetSelections(op.RootSelectionSet.GetPossibleTypes().First());
            IPreparedSelectionList droidSelections =
                op.GetSelections(rootSelections[0].SelectionSet!, droid);

            Assert.Equal("name", droidSelections[0].ResponseName);
            Assert.True(droidSelections[0].IsConditional);
            Assert.True(droidSelections[0].IsIncluded(variables.Object));
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            IPreparedSelectionList rootSelections =
                op.RootSelectionSet.GetSelections(op.RootSelectionSet.GetPossibleTypes().First());
            IPreparedSelectionList droidSelections =
                op.GetSelections(rootSelections[0].SelectionSet!, droid);

            Assert.Equal("name", droidSelections[0].ResponseName);
            Assert.False(droidSelections[0].IsConditional);
            Assert.True(droidSelections[0].IsIncluded(variables.Object));
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            IPreparedSelectionList rootSelections =
                op.RootSelectionSet.GetSelections(op.RootSelectionSet.GetPossibleTypes().First());
            IPreparedSelectionList droidSelections =
                op.GetSelections(rootSelections[0].SelectionSet!, droid);

            Assert.Collection(
                droidSelections.Where(t => t.IsIncluded(vFalse.Object)),
                t => Assert.Equal("id", t.ResponseName));

            Assert.Collection(
                droidSelections.Where(t => t.IsIncluded(vTrue.Object)),
                t => Assert.Equal("name", t.ResponseName),
                t => Assert.Equal("id", t.ResponseName),
                t => Assert.Equal("height", t.ResponseName));

            op.Print().MatchSnapshot();
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            IPreparedSelectionList rootSelections =
                op.RootSelectionSet.GetSelections(op.RootSelectionSet.GetPossibleTypes().First());
            IPreparedSelectionList droidSelections =
                op.GetSelections(rootSelections[0].SelectionSet!, droid);

            Assert.Collection(
                droidSelections.Where(t => t.IsIncluded(variables.Object)),
                t => Assert.Equal("id", t.ResponseName),
                t => Assert.Equal("height", t.ResponseName));

            op.Print().MatchSnapshot();
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation);

            // assert
            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            IPreparedSelectionList rootSelections =
                op.RootSelectionSet.GetSelections(op.RootSelectionSet.GetPossibleTypes().First());
            IPreparedSelectionList droidSelections =
                op.GetSelections(rootSelections[0].SelectionSet!, droid);

            Assert.Empty(droidSelections.Where(t => t.IsIncluded(variables.Object)));

            op.Print().MatchSnapshot();
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

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            var optimizers = new List<NoopOptimizer> { new NoopOptimizer() };

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                OperationCompiler.Compile(schema, fragments, operation, optimizers);

            // assert
            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);

            op.Print().MatchSnapshot();
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

        public class NoopOptimizer : ISelectionSetOptimizer
        {
            public void Optimize(SelectionSetOptimizerContext context)
            {
            }
        }

        public class SimpleOptimizer : ISelectionSetOptimizer
        {
            public void Optimize(SelectionSetOptimizerContext context)
            {
                if (context.FieldContext.TryPeek(out IObjectField field)
                    && field.Name.Equals("bar"))
                {
                    IObjectField baz = context.TypeContext.Fields["baz"];
                    FieldNode bazSelection = Utf8GraphQLParser.Syntax.ParseField("baz { text }");
                    FieldDelegate bazPipeline = context.CompileResolverPipeline(baz, bazSelection);

                    var compiledSelection = new PreparedSelection(
                        context.TypeContext,
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
