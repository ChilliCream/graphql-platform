using HotChocolate.Language;
using Moq;
using Xunit;

namespace HotChocolate.Execution.Processing;

public class VisibilityTests
{
    [Fact]
    public void TryExtract_Skip_With_Literal()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        FieldNode field = Utf8GraphQLParser.Syntax.ParseField("field @skip(if: true)");

        // act
        Visibility.TryExtract(field, out Visibility visibility);

        // assert
        Assert.False(visibility.IsVisible(variables.Object));
    }

    [Fact]
    public void Equals_Skip_With_Literal_True()
    {
        // arrange
        FieldNode fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        FieldNode fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: true)");

        // act
        Visibility.TryExtract(fieldA, out Visibility visibilityA);
        Visibility.TryExtract(fieldB, out Visibility visibilityB);

        // assert
        Assert.True(visibilityA.Equals(visibilityB));
    }

    [Fact]
    public void Equals_Skip_With_Variable_True()
    {
        // arrange
        FieldNode fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: $a)");
        FieldNode fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: $a)");

        // act
        Visibility.TryExtract(fieldA, out Visibility visibilityA);
        Visibility.TryExtract(fieldB, out Visibility visibilityB);

        // assert
        Assert.True(visibilityA.Equals(visibilityB));
    }

    [Fact]
    public void Equals_Skip_With_Literal_False()
    {
        // arrange
        FieldNode fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        FieldNode fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: false)");

        // act
        Visibility.TryExtract(fieldA, out Visibility visibilityA);
        Visibility.TryExtract(fieldB, out Visibility visibilityB);

        // assert
        Assert.False(visibilityA.Equals(visibilityB));
    }

    [Fact]
    public void Equals_Skip_With_Variable_False()
    {
        // arrange
        FieldNode fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        FieldNode fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: $a)");

        // act
        Visibility.TryExtract(fieldA, out Visibility visibilityA);
        Visibility.TryExtract(fieldB, out Visibility visibilityB);

        // assert
        Assert.False(visibilityA.Equals(visibilityB));
    }

    [Fact]
    public void TryExtract_False()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        FieldNode field = Utf8GraphQLParser.Syntax.ParseField("field @test(test: true)");

        // act
        var success = Visibility.TryExtract(field, out Visibility visibility);

        // assert
        Assert.False(success);
        Assert.True(visibility.IsVisible(variables.Object));
    }

    [Fact]
    public void TryExtract_False_2()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        FieldNode field = Utf8GraphQLParser.Syntax.ParseField("field");

        // act
        var success = Visibility.TryExtract(field, out Visibility visibility);

        // assert
        Assert.False(success);
        Assert.True(visibility.IsVisible(variables.Object));
    }

    [Fact]
    public void TryExtract_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        FieldNode field = Utf8GraphQLParser.Syntax.ParseField("field @skip(if: true)");

        // act
        var success = Visibility.TryExtract(field, out Visibility visibility);

        // assert
        Assert.True(success);
        Assert.False(visibility.IsVisible(variables.Object));
    }

    [Fact]
    public void GetHashCode_Skip_With_Literal_Equal()
    {
        // arrange
        FieldNode fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        FieldNode fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: true)");

        Visibility.TryExtract(fieldA, out Visibility visibilityA);
        Visibility.TryExtract(fieldB, out Visibility visibilityB);

        // act
        var hashCodeA = visibilityA.GetHashCode();
        var hashCodeB = visibilityB.GetHashCode();

        // assert
        Assert.Equal(hashCodeA, hashCodeB);
    }

    [Fact]
    public void GetHashCode_Skip_With_Literal_NotEqual()
    {
        // arrange
        FieldNode fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: true)");
        FieldNode fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: false)");

        Visibility.TryExtract(fieldA, out Visibility visibilityA);
        Visibility.TryExtract(fieldB, out Visibility visibilityB);

        // act
        var hashCodeA = visibilityA.GetHashCode();
        var hashCodeB = visibilityB.GetHashCode();

        // assert
        Assert.NotEqual(hashCodeA, hashCodeB);
    }

    [Fact]
    public void IsVisible_Skip_Variables_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(false);
        FieldNode field = Utf8GraphQLParser.Syntax.ParseField("field @skip(if: $a)");
        Visibility.TryExtract(field, out Visibility visibility);

        // act
        var visible = visibility.IsVisible(variables.Object);

        // assert
        Assert.True(visible);
    }

    [Fact]
    public void IsVisible_Include_Variables_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        variables.Setup(t => t.GetVariable<bool>(It.IsAny<NameString>())).Returns(true);
        FieldNode field = Utf8GraphQLParser.Syntax.ParseField("field @include(if: $a)");
        Visibility.TryExtract(field, out Visibility visibility);

        // act
        var visible = visibility.IsVisible(variables.Object);

        // assert
        Assert.True(visible);
    }

    [Fact]
    public void IsVisible_Include_Literal_True()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        FieldNode field = Utf8GraphQLParser.Syntax.ParseField("field @include(if: true)");
        Visibility.TryExtract(field, out Visibility visibility);

        // act
        var visible = visibility.IsVisible(variables.Object);

        // assert
        Assert.True(visible);
    }
}
