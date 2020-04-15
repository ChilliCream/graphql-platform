using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.StarWars.Models;
using HotChocolate.StarWars.Types;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Utilities
{
    public class VariableCoercionHelperTests
    {
        [Fact]
        public void VariableCoercionHelper_Schema_Is_Null()
        {
            // arrange
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
            Action action = () => helper.CoerceVariableValues(
                null, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void VariableCoercionHelper_VariableDefinitions_Is_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();
            var variableValues = new Dictionary<string, object>();
            var coercedValues = new Dictionary<string, VariableValue>();
            var helper = new VariableCoercionHelper();

            // act
            Action action = () => helper.CoerceVariableValues(
                schema, null, variableValues, coercedValues);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void VariableCoercionHelper_VariableValues_Is_Null()
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

            var coercedValues = new Dictionary<string, VariableValue>();

            var helper = new VariableCoercionHelper();

            // act
            Action action = () => helper.CoerceVariableValues(
                schema, variableDefinitions, null, coercedValues);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void VariableCoercionHelper_CoercedValues_Is_Null()
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

            var helper = new VariableCoercionHelper();

            // act
            Action action = () => helper.CoerceVariableValues(
                schema, variableDefinitions, variableValues, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

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

        [Fact]
        public void Coerce_Nullable_ReviewInput_Variable_With_Object_Literal()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("ReviewInput"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };

            var variableValues = new Dictionary<string, object>
            {
                {"abc", new ObjectValueNode(new ObjectFieldNode("stars", 5))}
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
                    Assert.Equal("ReviewInput", Assert.IsType<ReviewInputType>(t.Value.Type).Name);
                    Assert.Equal(5, Assert.IsType<Review>(t.Value.Value).Stars);
                    Assert.IsType<ObjectValueNode>(t.Value.ValueLiteral);
                });
        }

        [Fact]
        public void Coerce_Nullable_ReviewInput_Variable_With_Dictionary()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("ReviewInput"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };

            var variableValues = new Dictionary<string, object>
            {
                {"abc", new Dictionary<string, object> { {"stars", 5} }}
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
                    Assert.Equal("ReviewInput", Assert.IsType<ReviewInputType>(t.Value.Type).Name);
                    Assert.Equal(5, Assert.IsType<Review>(t.Value.Value).Stars);
                    Assert.Null(t.Value.ValueLiteral);
                });
        }

        [Fact]
        public void Coerce_Nullable_ReviewInput_Variable_With_Review_Object()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("ReviewInput"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };

            var variableValues = new Dictionary<string, object>
            {
                {"abc", new Review { Stars = 5 }}
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
                    Assert.Equal("ReviewInput", Assert.IsType<ReviewInputType>(t.Value.Type).Name);
                    Assert.Equal(5, Assert.IsType<Review>(t.Value.Value).Stars);
                    Assert.Null(t.Value.ValueLiteral);
                });
        }

        [Fact]
        public void Error_When_Value_Is_Null_On_Non_Null_Variable()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NonNullTypeNode(new NamedTypeNode("String")),
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
            Action action = () => helper.CoerceVariableValues(
                schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Throws<GraphQLException>(action).Errors.MatchSnapshot();
        }

        [Fact]
        public void Error_When_PlainValue_Is_Null_On_Non_Null_Variable()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NonNullTypeNode(new NamedTypeNode("String")),
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
            Action action = () => helper.CoerceVariableValues(
                schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Throws<GraphQLException>(action).Errors.MatchSnapshot();
        }

        [Fact]
        public void Error_When_Value_Type_Does_Not_Match_Variable_Type()
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
                {"abc", new IntValueNode(1)}
            };

            var coercedValues = new Dictionary<string, VariableValue>();

            var helper = new VariableCoercionHelper();

            // act
            Action action = () => helper.CoerceVariableValues(
                schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Throws<GraphQLException>(action).Errors.MatchSnapshot();
        }

        [Fact]
        public void Error_When_PlainValue_Type_Does_Not_Match_Variable_Type()
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
                {"abc", 1}
            };

            var coercedValues = new Dictionary<string, VariableValue>();

            var helper = new VariableCoercionHelper();

            // act
            Action action = () => helper.CoerceVariableValues(
                schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Throws<GraphQLException>(action).Errors.MatchSnapshot();
        }

        [Fact]
        public void Variable_Type_Is_Not_An_Input_Type()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("Human"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };

            var variableValues = new Dictionary<string, object>
            {
                {"abc", 1}
            };

            var coercedValues = new Dictionary<string, VariableValue>();

            var helper = new VariableCoercionHelper();

            // act
            Action action = () => helper.CoerceVariableValues(
                schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Throws<GraphQLException>(action).Errors.MatchSnapshot();
        }

        [Fact]
        public void Error_When_Input_Field_Has_Different_Properties_Than_Defined()
        {
            // arrange
            ISchema schema = SchemaBuilder.New().AddStarWarsTypes().Create();

            var variableDefinitions = new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("ReviewInput"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };

            var variableValues = new Dictionary<string, object>
            {
                {"abc", new ObjectValueNode(new ObjectFieldNode("abc", "def"))}
            };

            var coercedValues = new Dictionary<string, VariableValue>();

            var helper = new VariableCoercionHelper();

            // act
            Action action = () => helper.CoerceVariableValues(
                schema, variableDefinitions, variableValues, coercedValues);

            // assert
            Assert.Throws<GraphQLException>(action).Errors.MatchSnapshot();
        }
    }
}
