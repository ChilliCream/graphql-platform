using HotChocolate.Language;
using Moq;
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
    }
}
