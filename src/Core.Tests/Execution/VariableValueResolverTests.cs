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
    public class VariableValueResolverTests
    {
        [Fact]
        public void QueryWithNonNullVariableAndDefaultWhereValueWasProvided()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String! = \"foo\") { a }");
            Dictionary<string, IValueNode> variableValues =
                new Dictionary<string, IValueNode>();
            variableValues.Add("test", new StringValueNode(null, "123456", false));

            // act
            VariableValueResolver resolver = new VariableValueResolver();
            Dictionary<string, object> coercedVariableValues =
                resolver.CoerceVariableValues(schema, operation, variableValues);

            // assert
            Assert.True(coercedVariableValues.ContainsKey("test"));
            Assert.IsType<string>(coercedVariableValues["test"]);
            Assert.Equal("123456", coercedVariableValues["test"]);
        }

        [Fact]
        public void QueryWithNonNullVariableAndDefaultWhereValueWasNotProvided()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String! = \"foo\") { a }");
            Dictionary<string, IValueNode> variableValues =
                new Dictionary<string, IValueNode>();
            variableValues.Add("test", new NullValueNode(null));

            // act
            VariableValueResolver resolver = new VariableValueResolver();
            Action action = () =>
                resolver.CoerceVariableValues(schema, operation, variableValues);

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
            Dictionary<string, IValueNode> variableValues =
                new Dictionary<string, IValueNode>();

            // act
            VariableValueResolver resolver = new VariableValueResolver();
            Dictionary<string, object> coercedVariableValues =
                resolver.CoerceVariableValues(schema, operation, variableValues);

            // assert
            Assert.True(coercedVariableValues.ContainsKey("test"));
            Assert.IsType<string>(coercedVariableValues["test"]);
            Assert.Equal("foo", coercedVariableValues["test"]);
        }

        [Fact]
        public void QueryWithNullableVariableAndNoDefaultWhereNoValueWasProvided()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String) { a }");
            Dictionary<string, IValueNode> variableValues =
                new Dictionary<string, IValueNode>();
            variableValues.Add("test", new NullValueNode(null));

            // act
            VariableValueResolver resolver = new VariableValueResolver();
            Dictionary<string, object> coercedVariableValues =
                resolver.CoerceVariableValues(schema, operation, variableValues);

            // assert
            Assert.True(coercedVariableValues.ContainsKey("test"));
            Assert.Null(coercedVariableValues["test"]);
        }

        private Schema CreateSchema()
        {
            return Schema.Create("type Query { foo: Foo } type Foo { a: String }", c => { });
        }

        private OperationDefinitionNode CreateQuery(string query)
        {
            Parser parser = new Parser();
            return parser.Parse(query)
                .Definitions.OfType<OperationDefinitionNode>().First();
        }
    }
}
