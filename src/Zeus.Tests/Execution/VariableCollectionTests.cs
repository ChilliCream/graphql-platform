using System;
using System.Collections.Generic;
using Zeus.Abstractions;
using Zeus.Execution;
using Xunit;

namespace Zeus
{
    public class VariableCollectionTests
    {
        [Fact]
        public void NonNullVariableWithNull()
        {
            // arrange
            SchemaDocument schema = CreateSchema();
            OperationDefinition operation = new OperationDefinition(
                "doSomething", OperationType.Query,
                new[] { new VariableDefinition("x", NamedType.NonNullString) },
                new[] { new Field("getMeThis") });
            Dictionary<string, object> variableValues = new Dictionary<string, object>();

            // act
            Action action = () => new VariableCollection(
                schema, operation, variableValues);

            // assert
            Assert.Throws<GraphQLQueryException>(action);
        }

        [Fact]
        public void NullableVariableWithNull()
        {
            // arrange
            SchemaDocument schema = CreateSchema();
            OperationDefinition operation = new OperationDefinition(
                "doSomething", OperationType.Query,
                new[] { new VariableDefinition("x", NamedType.String) },
                new[] { new Field("getMeThis") });
            Dictionary<string, object> variableValues = new Dictionary<string, object>();

            // act
            VariableCollection variables = new VariableCollection(
                schema, operation, variableValues);

            // assert
            Assert.Null(variables.GetVariable<string>("x"));
        }

        [Fact]
        public void NullableVariableWithDefaultValue()
        {
            // arrange
            SchemaDocument schema = CreateSchema();
            OperationDefinition operation = new OperationDefinition(
                "doSomething", OperationType.Query,
                new[] { new VariableDefinition("x", NamedType.String, new StringValue("hello")) },
                new[] { new Field("getMeThis") });
            Dictionary<string, object> variableValues = new Dictionary<string, object>();

            // act
            VariableCollection variables = new VariableCollection(
                schema, operation, variableValues);

            // assert
            Assert.Equal("hello", variables.GetVariable<string>("x"));
        }

        [Fact]
        public void NullableVariableWithDefaultValueAndProvidedValue()
        {
            // arrange
            SchemaDocument schema = CreateSchema();
            OperationDefinition operation = new OperationDefinition(
                "doSomething", OperationType.Query,
                new[] { new VariableDefinition("x", NamedType.String, new StringValue("hello")) },
                new[] { new Field("getMeThis") });
            Dictionary<string, object> variableValues = new Dictionary<string, object>
            {
                {"x", "world"}
            };

            // act
            VariableCollection variables = new VariableCollection(
                schema, operation, variableValues);

            // assert
            Assert.Equal("world", variables.GetVariable<string>("x"));
        }

        [Fact]
        public void NullableVariableWithProvidedValue()
        {
            // arrange
            SchemaDocument schema = CreateSchema();
            OperationDefinition operation = new OperationDefinition(
                "doSomething", OperationType.Query,
                new[] { new VariableDefinition("x", NamedType.String) },
                new[] { new Field("getMeThis") });
            Dictionary<string, object> variableValues = new Dictionary<string, object>
            {
                {"x", "world"}
            };

            // act
            VariableCollection variables = new VariableCollection(
                schema, operation, variableValues);

            // assert
            Assert.Equal("world", variables.GetVariable<string>("x"));
        }

        [Fact]
        public void NonNullVariableWithProvidedValue()
        {
            // arrange
            SchemaDocument schema = CreateSchema();
            OperationDefinition operation = new OperationDefinition(
                "doSomething", OperationType.Query,
                new[] { new VariableDefinition("x", NamedType.NonNullString) },
                new[] { new Field("getMeThis") });
            Dictionary<string, object> variableValues = new Dictionary<string, object>
            {
                {"x", "world"}
            };

            // act
            VariableCollection variables = new VariableCollection(
                schema, operation, variableValues);

            // assert
            Assert.Equal("world", variables.GetVariable<string>("x"));
        }

        [Fact]
        public void NullableVariableWithValidInputValue()
        {
            // arrange
            SchemaDocument schema = CreateSchema();
            OperationDefinition operation = new OperationDefinition(
                "doSomething", OperationType.Query,
                new[] { new VariableDefinition("x", new NamedType("InputType")) },
                new[] { new Field("getMeThis") });
            Dictionary<string, object> variableValues = new Dictionary<string, object>
            {
                {"x", new Dictionary<string, object>
                        {
                            { "a", "hello" },
                            { "b", "world" }
                        }
                }
            };

            // act
            VariableCollection variables = new VariableCollection(
                schema, operation, variableValues);

            // assert
            IDictionary<string, object> inputObject = variables
                .GetVariable<IDictionary<string, object>>("x");
            Assert.NotNull(inputObject);
            Assert.Collection(inputObject,
                t =>
                {
                    Assert.Equal("a", t.Key);
                    Assert.Equal("hello", t.Value);
                },
                t =>
                {
                    Assert.Equal("b", t.Key);
                    Assert.Equal("world", t.Value);
                });
        }

        [Fact]
        public void NullableVariableWithInvalidInputValue()
        {
            // arrange
            SchemaDocument schema = CreateSchema();
            OperationDefinition operation = new OperationDefinition(
                "doSomething", OperationType.Query,
                new[] { new VariableDefinition("x", new NamedType("InputType")) },
                new[] { new Field("getMeThis") });
            Dictionary<string, object> variableValues = new Dictionary<string, object>
            {
                {"x", new Dictionary<string, object>
                        {
                            { "b", "world" }
                        }
                }
            };

            // act
            Action action = () => new VariableCollection(
                schema, operation, variableValues);

            // assert
            Assert.Throws<GraphQLQueryException>(action);
        }

        private SchemaDocument CreateSchema()
        {
            return new SchemaDocument(
                new ITypeDefinition[]
                {
                    new InputObjectTypeDefinition("InputType", new[]
                    {
                        new InputValueDefinition("a", NamedType.NonNullString),
                        new InputValueDefinition("b", NamedType.String)
                    }),
                    new ObjectTypeDefinition("Query", new []
                    {
                        new FieldDefinition("getMeThis", NamedType.String)
                    })
                });
        }
    }
}