using System.Collections.Generic;
using Zeus.Abstractions;
using Zeus.Execution;
using Xunit;

namespace Zeus
{
    public class CoerceVariableValuesTests
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
            QueryResult result = VariableHelper.CoerceVariableValues(schema, operation, variableValues);

            // assert
            Assert.Null(result.Data);
            Assert.NotNull(result.Errors);
            Assert.Collection(result.Errors,
                t => Assert.Equal($"Variable x mustn't be null.", t.Message));
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
            QueryResult result = VariableHelper.CoerceVariableValues(schema, operation, variableValues);

            // assert
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Collection(result.Data,
                t =>
                {
                    Assert.Equal("x", t.Key);
                    Assert.Null(t.Value);
                });
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
            QueryResult result = VariableHelper.CoerceVariableValues(schema, operation, variableValues);

            // assert
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Collection(result.Data,
                t =>
                {
                    Assert.Equal("x", t.Key);
                    Assert.Equal("hello", t.Value);
                });
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
            QueryResult result = VariableHelper.CoerceVariableValues(schema, operation, variableValues);

            // assert
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Collection(result.Data,
                t =>
                {
                    Assert.Equal("x", t.Key);
                    Assert.Equal("world", t.Value);
                });
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
            QueryResult result = VariableHelper.CoerceVariableValues(schema, operation, variableValues);

            // assert
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Collection(result.Data,
                t =>
                {
                    Assert.Equal("x", t.Key);
                    Assert.Equal("world", t.Value);
                });
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
            QueryResult result = VariableHelper.CoerceVariableValues(schema, operation, variableValues);

            // assert
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Collection(result.Data,
                t =>
                {
                    Assert.Equal("x", t.Key);
                    Assert.Equal("world", t.Value);
                });
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
            QueryResult result = VariableHelper.CoerceVariableValues(schema, operation, variableValues);

            // assert
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Collection(result.Data,
                t =>
                {
                    Assert.Equal("x", t.Key);
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
            QueryResult result = VariableHelper.CoerceVariableValues(schema, operation, variableValues);

            // assert            
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Collection(result.Errors,
                t => Assert.Equal($"Variable x mustn't be null.", t.Message));
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