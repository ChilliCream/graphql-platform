using HotChocolate.Language;
using Moq;

namespace HotChocolate.Execution.Processing;

public class VisibilityTests
{
    [Fact]
    public void TryExtract_Skip_With_Literal()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field @skip(if: true)");

        // act
        var includeCondition = IncludeCondition.FromSelection(field);

        // assert
        Assert.False(includeCondition.IsIncluded(variables.Object));
    }

    [Fact]
    public void Equals_Skip_With_Literal_True()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: true)");

        // act
        var includeConditionA = IncludeCondition.FromSelection(fieldA);
        var includeConditionB = IncludeCondition.FromSelection(fieldB);

        // assert
        Assert.True(includeConditionA.Equals(includeConditionB));
    }

    [Fact]
    public void Equals_Skip_With_Variable_True()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: $a)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: $a)");

        // act
        var includeConditionA = IncludeCondition.FromSelection(fieldA);
        var includeConditionB = IncludeCondition.FromSelection(fieldB);

        // assert
        Assert.True(includeConditionA.Equals(includeConditionB));
    }

    [Fact]
    public void Equals_Skip_With_Literal_False()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: false)");

        // act
        var includeConditionA = IncludeCondition.FromSelection(fieldA);
        var includeConditionB = IncludeCondition.FromSelection(fieldB);

        // assert
        Assert.False(includeConditionA.Equals(includeConditionB));
    }

    [Fact]
    public void Equals_Skip_With_Variable_False()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: $a)");

        // act
        var includeConditionA = IncludeCondition.FromSelection(fieldA);
        var includeConditionB = IncludeCondition.FromSelection(fieldB);

        // assert
        Assert.False(includeConditionA.Equals(includeConditionB));
    }

    [Fact]
    public void TryExtract_False()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field @test(test: true)");

        // act
        var includeCondition = IncludeCondition.FromSelection(field);

        // assert
        Assert.True(includeCondition.IsIncluded(variables.Object));
    }

    [Fact]
    public void TryExtract_False_2()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field");

        // act
        var includeCondition = IncludeCondition.FromSelection(field);

        // assert
        Assert.True(includeCondition.IsIncluded(variables.Object));
    }

    [Fact]
    public void TryExtract_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field @skip(if: true)");

        // act
        var includeCondition = IncludeCondition.FromSelection(field);

        // assert
        Assert.False(includeCondition.IsIncluded(variables.Object));
    }

    [Fact]
    public void GetHashCode_Skip_With_Literal_Equal()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: true)");

        var includeConditionA = IncludeCondition.FromSelection(fieldA);
        var includeConditionB = IncludeCondition.FromSelection(fieldB);

        // act
        var hashCodeA = includeConditionA.GetHashCode();
        var hashCodeB = includeConditionB.GetHashCode();

        // assert
        Assert.Equal(hashCodeA, hashCodeB);
    }

    [Fact]
    public void GetHashCode_Skip_With_Literal_NotEqual()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: false)");

        var includeConditionA = IncludeCondition.FromSelection(fieldA);
        var includeConditionB = IncludeCondition.FromSelection(fieldB);

        // act
        var hashCodeA = includeConditionA.GetHashCode();
        var hashCodeB = includeConditionB.GetHashCode();

        // assert
        Assert.NotEqual(hashCodeA, hashCodeB);
    }

    [Fact]
    public void IsVisible_Skip_Variables_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(false);
        var field = Utf8GraphQLParser.Syntax.ParseField("field @skip(if: $a)");
        var includeCondition = IncludeCondition.FromSelection(field);

        // act
        var visible = includeCondition.IsIncluded(variables.Object);

        // assert
        Assert.True(visible);
    }

    [Fact]
    public void IsVisible_Include_Variables_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<string>())).Returns(true);
        var field = Utf8GraphQLParser.Syntax.ParseField("field @include(if: $a)");
        var includeCondition = IncludeCondition.FromSelection(field);

        // act
        var visible = includeCondition.IsIncluded(variables.Object);

        // assert
        Assert.True(visible);
    }

    [Fact]
    public void IsVisible_Include_Literal_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field @include(if: true)");
        var includeCondition = IncludeCondition.FromSelection(field);

        // act
        var visible = includeCondition.IsIncluded(variables.Object);

        // assert
        Assert.True(visible);
    }
}
