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
            variableValues.Add("test", new StringValueNode(null, "123456", false));

            // act
            VariableValueBuilder resolver =
                new VariableValueBuilder(schema, operation);
            VariableCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal("123456", coercedVariableValues.GetVariable<string>("test"));
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
            Assert.Equal("foo", coercedVariableValues.GetVariable<string>("test"));
        }

        [Fact]
        public void QueryWithNullableVariableAndNoDefaultWhereNoValueWasProvided()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String) { a }");
            Dictionary<string, object> variableValues =
                new Dictionary<string, object>();
            variableValues.Add("test", NullValueNode.Default);

            // act
            VariableValueBuilder resolver =
                new VariableValueBuilder(schema, operation);
            VariableCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Null(coercedVariableValues.GetVariable<string>("test"));
        }

        private Schema CreateSchema()
        {
            return Schema.Create("type Query { foo: Foo } type Foo { a: String }",
                c => { c.Options.StrictValidation = false; });
        }

        private OperationDefinitionNode CreateQuery(string query)
        {
            Parser parser = new Parser();
            return parser.Parse(query)
                .Definitions.OfType<OperationDefinitionNode>().First();
        }
    }
}
