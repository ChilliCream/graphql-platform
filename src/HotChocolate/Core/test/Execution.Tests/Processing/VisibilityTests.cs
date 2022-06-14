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
    public void Not_Equals_Skip_With_Literal_True()
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
}
