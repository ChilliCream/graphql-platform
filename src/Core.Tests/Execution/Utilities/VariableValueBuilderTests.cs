using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Execution
{
    public class VariableValueBuilderTests
    {
        [Fact]
        public void QueryWithNonNullVariableAndDefaultWhereValueWasProvided()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String! = \"foo\") { a }");
            Dictionary<string, object> variableValues =
                new Dictionary<string, object>();
            variableValues.Add("test",
                new StringValueNode(null, "123456", false));

            // act
            VariableValueBuilder resolver =
                new VariableValueBuilder(schema, operation);
            VariableCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal("123456",
                coercedVariableValues.GetVariable<string>("test"));
        }

        [Fact]
        public void QueryWithNonNullVariableAndDefaultWhereValueWasNotProvided()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String! = \"foo\") { a }");
            Dictionary<string, object> variableValues =
                new Dictionary<string, object>();
            variableValues.Add("test", NullValueNode.Default);

            // act
            VariableValueBuilder resolver =
                new VariableValueBuilder(schema, operation);
            Action action = () => resolver.CreateValues(variableValues);

            // assert
            Assert.Throws<QueryException>(action);
        }

        [Fact]
        public void QueryWithNonNullVariableAndDefaultWhereValueIsNull()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String! = \"foo\") { a }");
            Dictionary<string, object> variableValues =
                new Dictionary<string, object>();

            // act
            VariableValueBuilder resolver =
                new VariableValueBuilder(schema, operation);
            VariableCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal("foo",
                coercedVariableValues.GetVariable<string>("test"));
        }

        [Fact]
        public void QueryWithNullableVariableAndNoDefaultWhereNoValueWasProvided()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String) { a }");
            var variableValues = new Dictionary<string, object>();
            variableValues.Add("test", NullValueNode.Default);

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Null(coercedVariableValues.GetVariable<string>("test"));
        }

        [Fact]
        public void CoerceEnumFromString()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarEnum!) { a }");

            var variableValues = new Dictionary<string, object>();
            variableValues.Add("test", "A");

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal(BarEnum.A,
                coercedVariableValues.GetVariable<BarEnum>("test"));
        }

        [Fact]
        public void CoerceEnumFromStringValueNode()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarEnum!) { a }");

            var variableValues = new Dictionary<string, object>();
            variableValues.Add("test", new StringValueNode("A"));

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal(BarEnum.A,
                coercedVariableValues.GetVariable<BarEnum>("test"));
        }

        [Fact]
        public void CoerceEnumFromEnumValueNode()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarEnum!) { a }");

            var variableValues = new Dictionary<string, object>();
            variableValues.Add("test", new EnumValueNode("A"));

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal(BarEnum.A,
                coercedVariableValues.GetVariable<BarEnum>("test"));
        }

        [Fact]
        public void CoerceInputObjectWithEnumAsStringValueNode()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarInput!) { a }");

            ObjectValueNode fooInput = new ObjectValueNode(
                new ObjectFieldNode("b",
                    new StringValueNode("B")));

            ObjectValueNode barInput = new ObjectValueNode(
                new ObjectFieldNode("f", fooInput));

            var variableValues = new Dictionary<string, object>();
            variableValues.Add("test", barInput);

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Bar bar = coercedVariableValues.GetVariable<Bar>("test");
            Assert.NotNull(bar.F);
            Assert.Equal(BarEnum.B, bar.F.B);
        }

        [Fact]
        public void CoerceInputObjectWithEnumAsEnumValueNode()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarInput!) { a }");

            ObjectValueNode fooInput = new ObjectValueNode(
                new ObjectFieldNode("b",
                    new EnumValueNode("B")));

            ObjectValueNode barInput = new ObjectValueNode(
                new ObjectFieldNode("f", fooInput));

            var variableValues = new Dictionary<string, object>();
            variableValues.Add("test", barInput);

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Bar bar = coercedVariableValues.GetVariable<Bar>("test");
            Assert.NotNull(bar.F);
            Assert.Equal(BarEnum.B, bar.F.B);
        }

        private Schema CreateSchema()
        {
            return Schema.Create(
                "type Query { foo: Foo } type Foo { a: String } ",
                c =>
                {
                    c.RegisterType<BarType>();
                    c.RegisterType<FooType>();
                    c.RegisterType<BarEnumType>();
                    c.Options.StrictValidation = false;
                });
        }

        private OperationDefinitionNode CreateQuery(string query)
        {
            Parser parser = new Parser();
            return parser.Parse(query)
                .Definitions.OfType<OperationDefinitionNode>().First();
        }

        public class BarType
            : InputObjectType<Bar>
        {
        }

        public class FooType
            : InputObjectType<Foo>
        {
        }

        public class BarEnumType
            : EnumType<BarEnum>
        {
        }

        public class Bar
        {
            public Foo F { get; set; }
        }

        public class Foo
        {
            public BarEnum B { get; set; }
        }

        public enum BarEnum
        {
            A,
            B
        }
    }
}
