using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution.Utilities
{
    public class VariableCoercionHelperTests
    {
        [Fact]
        public void Coerce_Nullable_String_Variable_With_Default_Where_Value_Is_Not_Provided()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("String"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };

            var variableValues = new Dictionary<string, object>();
            var coercedValues = new Dictionary<string, VariableValue>();

            var helper = new VariableCoercionHelper();

            // act
            helper.CoerceVariableValues(schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Collection(coercedValues,
                t =>
                {
                    Assert.Equal("abc", t.Key);
                    Assert.Equal("String", Assert.IsType<StringType>(t.Value.Type).Name);
                    Assert.Equal("def", t.Value.Value);
                    Assert.Equal("def", Assert.IsType<StringValueNode>(t.Value.ValueLiteral).Value);
                });
        }

        [Fact]
        public void Coerce_Nullable_String_Variable_With_Default_Where_Value_Is_Provided()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("String"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };

            var variableValues = new Dictionary<string, object>
            {
                {"abc", new StringValueNode("xyz")}
            };

            var coercedValues = new Dictionary<string, VariableValue>();

            var helper = new VariableCoercionHelper();

            // act
            helper.CoerceVariableValues(schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Collection(coercedValues,
                t =>
                {
                    Assert.Equal("abc", t.Key);
                    Assert.Equal("String", Assert.IsType<StringType>(t.Value.Type).Name);
                    Assert.Equal("xyz", t.Value.Value);
                    Assert.Equal("xyz", Assert.IsType<StringValueNode>(t.Value.ValueLiteral).Value);
                });
        }

        [Fact]
        public void Coerce_Nullable_String_Variable_With_Default_Where_Plain_Value_Is_Provided()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("String"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };

            var variableValues = new Dictionary<string, object>
            {
                {"abc", "xyz"}
            };

            var coercedValues = new Dictionary<string, VariableValue>();

            var helper = new VariableCoercionHelper();

            // act
            helper.CoerceVariableValues(schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Collection(coercedValues,
                t =>
                {
                    Assert.Equal("abc", t.Key);
                    Assert.Equal("String", Assert.IsType<StringType>(t.Value.Type).Name);
                    Assert.Equal("xyz", t.Value.Value);
                    Assert.Null(t.Value.ValueLiteral);
                });
        }

        [Fact]
        public void Coerce_Nullable_String_Variable_With_Default_Where_Null_Is_Provided()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("String"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };

            var variableValues = new Dictionary<string, object>
            {
                {"abc", NullValueNode.Default}
            };

            var coercedValues = new Dictionary<string, VariableValue>();

            var helper = new VariableCoercionHelper();

            // act
            helper.CoerceVariableValues(schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Collection(coercedValues,
                t =>
                {
                    Assert.Equal("abc", t.Key);
                    Assert.Equal("String", Assert.IsType<StringType>(t.Value.Type).Name);
                    Assert.Null(t.Value.Value);
                    Assert.IsType<NullValueNode>(t.Value.ValueLiteral);
                });
        }

        [Fact]
        public void Coerce_Nullable_String_Variable_With_Default_Where_Plain_Null_Is_Provided()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("String"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };

            var variableValues = new Dictionary<string, object>
            {
                {"abc", null}
            };

            var coercedValues = new Dictionary<string, VariableValue>();

            var helper = new VariableCoercionHelper();

            // act
            helper.CoerceVariableValues(schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Collection(coercedValues,
                t =>
                {
                    Assert.Equal("abc", t.Key);
                    Assert.Equal("String", Assert.IsType<StringType>(t.Value.Type).Name);
                    Assert.Null(t.Value.Value);
                    Assert.IsType<NullValueNode>(t.Value.ValueLiteral);
                });
        }
    }
}
