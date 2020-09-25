using System;
using HotChocolate.Language;
using Moq;
using Xunit;

namespace HotChocolate.Execution.Processing
{
    public class SelectionIncludeConditionTests
    {
        [Fact]
        public void Skip_True_Is_Visible_False()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            var visibility = new SelectionIncludeCondition(skip: new BooleanValueNode(true));

            // act
            var visible = visibility.IsTrue(variableValues.Object);

            // assert
            Assert.False(visible);
        }

        [Fact]
        public void Skip_Var_True_Is_Visible_False()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(true);
            var visibility = new SelectionIncludeCondition(skip: new VariableNode("b"));

            // act
            var visible = visibility.IsTrue(variableValues.Object);

            // assert
            Assert.False(visible);
        }

        [Fact]
        public void Skip_False_Is_Visible_True()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            var visibility = new SelectionIncludeCondition(skip: new BooleanValueNode(false));

            // act
            var visible = visibility.IsTrue(variableValues.Object);

            // assert
            Assert.True(visible);
        }

        [Fact]
        public void Skip_Var_False_Is_Visible_True()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(false);
            var visibility = new SelectionIncludeCondition(skip: new VariableNode("b"));

            // act
            var visible = visibility.IsTrue(variableValues.Object);

            // assert
            Assert.True(visible);
        }

        [Fact]
        public void Include_True_Is_Visible_True()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            var visibility = new SelectionIncludeCondition(include: new BooleanValueNode(true));

            // act
            var visible = visibility.IsTrue(variableValues.Object);

            // assert
            Assert.True(visible);
        }

        [Fact]
        public void Include_Var_True_Is_Visible_True()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(true);
            var visibility = new SelectionIncludeCondition(include: new VariableNode("b"));

            // act
            var visible = visibility.IsTrue(variableValues.Object);

            // assert
            Assert.True(visible);
        }

        [Fact]
        public void Include_False_Is_Visible_False()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            var visibility = new SelectionIncludeCondition(include: new BooleanValueNode(false));

            // act
            var visible = visibility.IsTrue(variableValues.Object);

            // assert
            Assert.False(visible);
        }

        [Fact]
        public void Include_Var_False_Is_Visible_False()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();
            variableValues.Setup(t => t.GetVariable<bool>("b")).Returns(false);
            var visibility = new SelectionIncludeCondition(include: new VariableNode("b"));

            // act
            var visible = visibility.IsTrue(variableValues.Object);

            // assert
            Assert.False(visible);
        }

        [Fact]
        public void Parent_Visible_True()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();

            var parent = new SelectionIncludeCondition(
                include: new BooleanValueNode(true));

            var visibility = new SelectionIncludeCondition(
                include: new BooleanValueNode(true),
                parent: parent);

            // act
            var visible = visibility.IsTrue(variableValues.Object);

            // assert
            Assert.True(visible);
        }

        [Fact]
        public void Parent_Visible_False()
        {
            // arrange
            var variableValues = new Mock<IVariableValueCollection>();

            var parent = new SelectionIncludeCondition(
                include: new BooleanValueNode(false));

            var visibility = new SelectionIncludeCondition(
                include: new BooleanValueNode(true),
                parent: parent);

            // act
            var visible = visibility.IsTrue(variableValues.Object);

            // assert
            Assert.False(visible);
        }

        [Fact]
        public void Include_Is_String_GraphQLException()
        {
            // arrange
            // act
            Action action = () => new SelectionIncludeCondition(
                include: new StringValueNode("abc"));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void Skip_Is_String_GraphQLException()
        {
            // arrange
            // act
            Action action = () => new SelectionIncludeCondition(
                skip: new StringValueNode("abc"));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void Equals_Include_True_vs_True()
        {
            // arrange
            var a = new SelectionIncludeCondition(
                include: new BooleanValueNode(true));

            var b = new SelectionIncludeCondition(
                include: new BooleanValueNode(true));

            // act
            var equals = a.Equals(b);

            // assert
            Assert.True(equals);
        }

        [Fact]
        public void Equals_Include_True_vs_False()
        {
            // arrange
            var a = new SelectionIncludeCondition(
                include: new BooleanValueNode(true));

            var b = new SelectionIncludeCondition(
                include: new BooleanValueNode(false));

            // act
            var equals = a.Equals(b);

            // assert
            Assert.False(equals);
        }

        [Fact]
        public void Equals_Include_True_vs_Variable()
        {
            // arrange
            var a = new SelectionIncludeCondition(
                include: new BooleanValueNode(true));

            var b = new SelectionIncludeCondition(
                include: new VariableNode("b"));

            // act
            var equals = a.Equals(b);

            // assert
            Assert.False(equals);
        }

        [Fact]
        public void Equals_Include_Variable_A_vs_Variable_B()
        {
            // arrange
            var a = new SelectionIncludeCondition(
                include: new VariableNode("a"));

            var b = new SelectionIncludeCondition(
                include: new VariableNode("b"));

            // act
            var equals = a.Equals(b);

            // assert
            Assert.False(equals);
        }

        [Fact]
        public void Equals_Include_Variable_A_vs_Variable_A()
        {
            // arrange
            var a = new SelectionIncludeCondition(
                include: new VariableNode("a"));

            var b = new SelectionIncludeCondition(
                include: new VariableNode("a"));

            // act
            var equals = a.Equals(b);

            // assert
            Assert.True(equals);
        }
    }
}
