
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class ArgumentValueBuilderTests
    {
        [Fact]
        public void Coerce_NonNullString_ToAbc()
        {
            // arrange
            var variables = new Mock<IVariableCollection>();
            ISchema schema = CreateSchema();
            FieldNode fieldNode = CreateField("bar(a: \"abc\")");
            var selection = FieldSelection.Create(
                fieldNode,
                schema.QueryType.Fields["bar"],
                "bar");
            var path = Path.New("bar");

            // act
            Dictionary<string, ArgumentValue> arguments =
                ArgumentValueBuilder.CoerceArgumentValues(
                    selection, variables.Object, path);

            // assert
            MatchSnapshot(arguments);
        }

        [Fact]
        public void Coerce_NonNullString_ToNull()
        {
            // arrange
            var variables = new Mock<IVariableCollection>();
            ISchema schema = CreateSchema();
            FieldNode fieldNode = CreateField("bar");
            var selection = FieldSelection.Create(
                fieldNode,
                schema.QueryType.Fields["bar"],
                "bar");
            var path = Path.New("bar");

            // act
            Action action = () =>
                ArgumentValueBuilder.CoerceArgumentValues(
                    selection, variables.Object, path);

            // assert
            Assert.Throws<QueryException>(action).Errors.MatchSnapshot();
        }

        [Fact]
        public void Coerce_InputObject_NonNullFieldIsNull()
        {
            // arrange
            var variables = new Mock<IVariableCollection>();
            ISchema schema = CreateSchema();
            FieldNode fieldNode = CreateField("foo(a: {  a: { } })");
            var selection = FieldSelection.Create(
                fieldNode,
                schema.QueryType.Fields["foo"],
                "foo");
            var path = Path.New("bar");

            // act
            Action action = () =>
                ArgumentValueBuilder.CoerceArgumentValues(
                    selection, variables.Object, path);

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
            return Parser.Default.Parse($"{{ {field} }}").Definitions
                .OfType<OperationDefinitionNode>()
                .SelectMany(t => t.SelectionSet.Selections)
                .OfType<FieldNode>()
                .First();
        }

        private void MatchSnapshot(Dictionary<string, ArgumentValue> args)
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
