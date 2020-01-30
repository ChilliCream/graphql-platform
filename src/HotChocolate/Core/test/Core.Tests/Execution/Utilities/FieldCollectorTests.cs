using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class FieldCollectorTests
    {
        [Fact]
        public void MergeFieldsWithFragmentSpreads()
        {
            // arrange
            DocumentNode query = Utf8GraphQLParser.Parse(
                FileResource.Open("MergeQuery.graphql"));

            OperationDefinitionNode operation =
                query.Definitions.OfType<OperationDefinitionNode>().Single();

            var schema = Schema.Create(
                FileResource.Open("MergeSchema.graphql"),
                c => c.Use(next => context => Task.CompletedTask));

            var fragments = new FragmentCollection(schema, query);

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());

            // act
            var collector = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> selections =
                collector.CollectFields(schema.QueryType,
                    operation.SelectionSet, Path.New("foo"));

            // assert
            Assert.Collection(selections,
                selection =>
                {
                    Assert.Collection(selection.Selection.SelectionSet.Selections,
                        selectionNode =>
                        {
                            FragmentSpreadNode fragment = Assert.IsType<FragmentSpreadNode>(
                                selectionNode);
                            Assert.Equal("app", fragment.Name.Value);
                        },
                        selectionNode =>
                        {
                            FragmentSpreadNode fragment = Assert.IsType<FragmentSpreadNode>(
                                selectionNode);
                            Assert.Equal("parts", fragment.Name.Value);
                        });
                });
        }

        [Fact]
        public void MergeMerged()
        {
            // arrange
            DocumentNode query = Utf8GraphQLParser.Parse(
                FileResource.Open("MergeQuery.graphql"));

            OperationDefinitionNode operation =
                query.Definitions.OfType<OperationDefinitionNode>().Single();

            var schema = Schema.Create(
                FileResource.Open("MergeSchema.graphql"),
                c => c.Use(next => context => Task.CompletedTask));

            var fragments = new FragmentCollection(schema, query);

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());

            var collector = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> selections =
                collector.CollectFields(schema.QueryType,
                    operation.SelectionSet, Path.New("foo"));

            // act
            selections = collector.CollectFields(
                schema.GetType<ObjectType>("Application"),
                selections.Single().Selection.SelectionSet,
                Path.New("bat"));

            // assert
            Assert.Collection(selections,
                selection => Assert.Equal("id", selection.ResponseName),
                selection => Assert.Equal("name", selection.ResponseName),
                selection => Assert.Equal("parts", selection.ResponseName));
        }

        [Fact]
        public void Coerce_NonNullString_ToAbc()
        {
            // arrange
            DocumentNode document =
                Utf8GraphQLParser.Parse("{ bar (a: \"abc\") }");
            OperationDefinitionNode operation = document.Definitions
                .OfType<OperationDefinitionNode>().First();

            ISchema schema = CreateSchema();
            var fragments = new FragmentCollection(
                schema,
                document);

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());

            var collector = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> selections =
                collector.CollectFields(schema.QueryType,
                    operation.SelectionSet, Path.New("bar"));
            FieldSelection selection = selections.First();
            var path = Path.New("bar");

            // act
            IReadOnlyDictionary<NameString, ArgumentValue> arguments =
                selection.CoerceArguments(variables, TypeConversion.Default);

            // assert
            MatchSnapshot(arguments);
        }

        [Fact]
        public void Coerce_NonNullString_ToNull()
        {
            // arrange
            DocumentNode document =
               Utf8GraphQLParser.Parse("{ bar }");
            OperationDefinitionNode operation = document.Definitions
                .OfType<OperationDefinitionNode>().First();

            ISchema schema = CreateSchema();
            var fragments = new FragmentCollection(
                schema,
                document);

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());

            var collector = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> selections =
                collector.CollectFields(schema.QueryType,
                    operation.SelectionSet, Path.New("bar"));
            FieldSelection selection = selections.First();
            var path = Path.New("bar");

            // act
            Action action = () =>
                selection.CoerceArguments(variables, TypeConversion.Default);

            // assert
            Assert.Throws<QueryException>(action).Errors.MatchSnapshot();
        }

        [Fact]
        public void Coerce_InputObject_NonNullFieldIsNull()
        {
            // arrange
            DocumentNode document =
                Utf8GraphQLParser.Parse("{ foo(a: {  a: { } }) }");
            OperationDefinitionNode operation = document.Definitions
                .OfType<OperationDefinitionNode>().First();

            ISchema schema = CreateSchema();
            var fragments = new FragmentCollection(
                schema,
                document);

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());

            var collector = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());
            IReadOnlyCollection<FieldSelection> selections =
                collector.CollectFields(schema.QueryType,
                    operation.SelectionSet, null);
            FieldSelection selection = selections.First();

            // act
            Action action = () =>
                selection.CoerceArguments(variables, TypeConversion.Default);

            // assert
            Assert.Throws<QueryException>(action).Errors.MatchSnapshot();
        }

        private static ISchema CreateSchema()
        {
            return Schema.Create(
                FileResource.Open("ArgumentValueBuilder.graphql"),
                c => c.Use(next => context => Task.CompletedTask));
        }

        private static FieldNode CreateField(string field)
        {
            return Utf8GraphQLParser.Parse($"{{ {field} }}").Definitions
                .OfType<OperationDefinitionNode>()
                .SelectMany(t => t.SelectionSet.Selections)
                .OfType<FieldNode>()
                .First();
        }

        private void MatchSnapshot(
            IReadOnlyDictionary<NameString, ArgumentValue> args)
        {
            args.Select(t => new ArgumentValueSnapshot(t.Key, t.Value))
                .ToList().MatchSnapshot();
        }

        private class ArgumentValueSnapshot
        {
            public ArgumentValueSnapshot(
                string name,
                ArgumentValue argumentValue)
            {
                Name = name;
                Type = TypeVisualizer.Visualize(argumentValue.Type);
                Value = argumentValue.Value;
            }

            public string Name { get; }

            public string Type { get; }

            public object Value { get; }
        }
    }
}
