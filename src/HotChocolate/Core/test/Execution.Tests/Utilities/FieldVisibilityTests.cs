using System;
using HotChocolate.Language;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Utilities
{
    public class FieldVisibilityTests
    {
        [Fact]
        public void Skip_True_Is_Visible_False()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            var visibility = new FieldVisibility(skip: new BooleanValueNode(true));

            // act
            bool visible = visibility.IsVisible(variableValues.Object);

            // assert
            Assert.False(visible);
        }

        [Fact]
        public void Skip_Var_True_Is_Visible_False()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(true);
            var visibility = new FieldVisibility(skip: new VariableNode("b"));

            // act
            bool visible = visibility.IsVisible(variableValues.Object);

            // assert
            Assert.False(visible);
        }

        [Fact]
        public void Skip_False_Is_Visible_True()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            var visibility = new FieldVisibility(skip: new BooleanValueNode(false));

            // act
            bool visible = visibility.IsVisible(variableValues.Object);

            // assert
            Assert.True(visible);
        }

        [Fact]
        public void Skip_Var_False_Is_Visible_True()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(false);
            var visibility = new FieldVisibility(skip: new VariableNode("b"));

            // act
            bool visible = visibility.IsVisible(variableValues.Object);

            // assert
            Assert.True(visible);
        }

        [Fact]
        public void Inclide_True_Is_Visible_True()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            var visibility = new FieldVisibility(include: new BooleanValueNode(true));

            // act
            bool visible = visibility.IsVisible(variableValues.Object);

            // assert
            Assert.True(visible);
        }

        [Fact]
        public void Inclide_Var_True_Is_Visible_True()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(true);
            var visibility = new FieldVisibility(include: new VariableNode("b"));

            // act
            bool visible = visibility.IsVisible(variableValues.Object);

            // assert
            Assert.True(visible);
        }

        [Fact]
        public void Include_False_Is_Visible_False()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            var visibility = new FieldVisibility(include: new BooleanValueNode(false));

            // act
            bool visible = visibility.IsVisible(variableValues.Object);

            // assert
            Assert.False(visible);
        }

        [Fact]
        public void Include_Var_False_Is_Visible_False()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(false);
            var visibility = new FieldVisibility(include: new VariableNode("b"));

            // act
            bool visible = visibility.IsVisible(variableValues.Object);

            // assert
            Assert.False(visible);
        }

        [Fact]
        public void Parent_Visible_True()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();

            var parent = new FieldVisibility(
                include: new BooleanValueNode(true));

            var visibility = new FieldVisibility(
                include: new BooleanValueNode(true),
                parent: parent);

            // act
            bool visible = visibility.IsVisible(variableValues.Object);

            // assert
            Assert.True(visible);
        }

        [Fact]
        public void Parent_Visible_False()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();

            var parent = new FieldVisibility(
                include: new BooleanValueNode(false));

            var visibility = new FieldVisibility(
                include: new BooleanValueNode(true),
                parent: parent);

            // act
            bool visible = visibility.IsVisible(variableValues.Object);

            // assert
            Assert.False(visible);
        }

        [Fact]
        public void Include_Is_String_GraphQLException()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            var visibility = new FieldVisibility(
                include: new StringValueNode("abc"));

            // act
            Action action = () => visibility.IsVisible(variableValues.Object);

            // assert
            Assert.Throws<GraphQLException>(action).Errors.MatchSnapshot(); ;
        }

        [Fact]
        public void Skip_Is_String_GraphQLException()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            var visibility = new FieldVisibility(
                skip: new StringValueNode("abc"));

            // act
            Action action = () => visibility.IsVisible(variableValues.Object);

            // assert
            Assert.Throws<GraphQLException>(action).Errors.MatchSnapshot(); ;
        }

        [Fact]
        public void Equals_Include_True_vs_True()
        {
            // arrange
            var a = new FieldVisibility(
                include: new BooleanValueNode(true));

            var b = new FieldVisibility(
                include: new BooleanValueNode(true));

            // act
            bool equals = a.Equals(b);

            // assert
            Assert.True(equals);
        }

        [Fact]
        public void Equals_Include_True_vs_False()
        {
            // arrange
            var a = new FieldVisibility(
                include: new BooleanValueNode(true));

            var b = new FieldVisibility(
                include: new BooleanValueNode(false));

            // act
            bool equals = a.Equals(b);

            // assert
            Assert.False(equals);
        }

        [Fact]
        public void Equals_Include_True_vs_Variable()
        {
            // arrange
            var a = new FieldVisibility(
                include: new BooleanValueNode(true));

            var b = new FieldVisibility(
                include: new VariableNode("b"));

            // act
            bool equals = a.Equals(b);

            // assert
            Assert.False(equals);
        }

        [Fact]
        public void Equals_Include_Variable_A_vs_Variable_B()
        {
            // arrange
            var a = new FieldVisibility(
                include: new VariableNode("a"));

            var b = new FieldVisibility(
                include: new VariableNode("b"));

            // act
            bool equals = a.Equals(b);

            // assert
            Assert.False(equals);
        }

        [Fact]
        public void Equals_Include_Variable_A_vs_Variable_A()
        {
            // arrange
            var a = new FieldVisibility(
                include: new VariableNode("a"));

            var b = new FieldVisibility(
                include: new VariableNode("a"));

            // act
            bool equals = a.Equals(b);

            // assert
            Assert.True(equals);
        }
    }
}
