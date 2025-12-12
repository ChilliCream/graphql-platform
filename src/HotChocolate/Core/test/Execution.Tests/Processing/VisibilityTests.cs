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
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.True(hasIncludeCondition);
        Assert.False(includeCondition.IsIncluded(variables.Object));
    }

    [Fact]
    public void Equals_Skip_With_Literal_True()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: true)");

        // act
        var hasIncludeConditionA = IncludeCondition.TryCreate(fieldA, out var includeConditionA);
        var hasIncludeConditionB = IncludeCondition.TryCreate(fieldB, out var includeConditionB);

        // assert
        Assert.True(hasIncludeConditionA);
        Assert.True(hasIncludeConditionB);
        Assert.True(includeConditionA.Equals(includeConditionB));
    }

    [Fact]
    public void Equals_Skip_With_Variable_True()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: $a)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: $a)");

        // act
        var hasIncludeConditionA = IncludeCondition.TryCreate(fieldA, out var includeConditionA);
        var hasIncludeConditionB = IncludeCondition.TryCreate(fieldB, out var includeConditionB);

        // assert
        Assert.True(hasIncludeConditionA);
        Assert.True(hasIncludeConditionB);
        Assert.True(includeConditionA.Equals(includeConditionB));
    }

    [Fact]
    public void Equals_Skip_With_Literal_False()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: false)");

        // act
        var hasIncludeConditionA = IncludeCondition.TryCreate(fieldA, out var includeConditionA);
        var hasIncludeConditionB = IncludeCondition.TryCreate(fieldB, out var includeConditionB);

        // assert
        Assert.True(hasIncludeConditionA);
        Assert.True(hasIncludeConditionB);
        Assert.False(includeConditionA.Equals(includeConditionB));
    }

    [Fact]
    public void Equals_Skip_With_Variable_False()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: $a)");

        // act
        var hasIncludeConditionA = IncludeCondition.TryCreate(fieldA, out var includeConditionA);
        var hasIncludeConditionB = IncludeCondition.TryCreate(fieldB, out var includeConditionB);

        // assert
        Assert.True(hasIncludeConditionA);
        Assert.True(hasIncludeConditionB);
        Assert.False(includeConditionA.Equals(includeConditionB));
    }

    [Fact]
    public void TryExtract_False()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field @test(test: true)");

        // act
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.False(hasIncludeCondition);
    }

    [Fact]
    public void TryExtract_False_2()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field");

        // act
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.False(hasIncludeCondition);
    }

    [Fact]
    public void TryExtract_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field @skip(if: true)");

        // act
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.True(hasIncludeCondition);
        Assert.False(includeCondition.IsIncluded(variables.Object));
    }

    [Fact]
    public void GetHashCode_Skip_With_Literal_Equal()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: true)");

        var hasIncludeConditionA = IncludeCondition.TryCreate(fieldA, out var includeConditionA);
        var hasIncludeConditionB = IncludeCondition.TryCreate(fieldB, out var includeConditionB);

        // act
        var hashCodeA = includeConditionA.GetHashCode();
        var hashCodeB = includeConditionB.GetHashCode();

        // assert
        Assert.True(hasIncludeConditionA);
        Assert.True(hasIncludeConditionB);
        Assert.Equal(hashCodeA, hashCodeB);
    }

    [Fact]
    public void GetHashCode_Skip_With_Literal_NotEqual()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: false)");

        var hasIncludeConditionA = IncludeCondition.TryCreate(fieldA, out var includeConditionA);
        var hasIncludeConditionB = IncludeCondition.TryCreate(fieldB, out var includeConditionB);

        // act
        var hashCodeA = includeConditionA.GetHashCode();
        var hashCodeB = includeConditionB.GetHashCode();

        // assert
        Assert.True(hasIncludeConditionA);
        Assert.True(hasIncludeConditionB);
        Assert.NotEqual(hashCodeA, hashCodeB);
    }

    [Fact]
    public void IsVisible_Skip_Variables_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetValue<BooleanValueNode>(It.IsAny<string>())).Returns(BooleanValueNode.False);
        var field = Utf8GraphQLParser.Syntax.ParseField("field @skip(if: $a)");

        // act
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.True(hasIncludeCondition);
        Assert.True(includeCondition.IsIncluded(variables.Object));
    }

    [Fact]
    public void IsVisible_Include_Variables_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetValue<BooleanValueNode>(It.IsAny<string>())).Returns(BooleanValueNode.True);
        var field = Utf8GraphQLParser.Syntax.ParseField("field @include(if: $a)");

        // act
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.True(hasIncludeCondition);
        Assert.True(includeCondition.IsIncluded(variables.Object));
    }

    [Fact]
    public void IsVisible_Include_Literal_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field @include(if: true)");

        // act
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.True(hasIncludeCondition);
        Assert.True(includeCondition.IsIncluded(variables.Object));
    }
}
