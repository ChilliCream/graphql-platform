using Xunit;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Language.SyntaxTree;

public class FieldDefinitionNodeTests
{
    [Fact]
    public void Equals_FieldDefinitionNode_When_Both_Are_Equal()
    {
        // arrange
        FieldDefinitionNode a = ParseFieldDefinition("foo(a: String): String @bar");
        FieldDefinitionNode b = ParseFieldDefinition("foo(a: String): String @bar");

        // act
        var success = a.Equals(b);

        // assert
        Assert.True(success);
    }

    [Fact]
    public void Equals_With_Same_Location()
    {
        var a = ParseFieldDefinition("foo(a: String): String @bar");
        var b = ParseFieldDefinition("foo(a: String): String @bar");
        var c = ParseFieldDefinition("bar(a: String): String @bar");

        // act
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var acResult = a.Equals(c);
        var aNullResult = a.Equals(default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void Equals_With_Different_Location()
    {
        // arrange
        var a = ParseFieldDefinition("foo(a: String): String @bar");
        var b = ParseFieldDefinition("   foo  (a : String): String @bar");
        var c = ParseFieldDefinition("bar(a: String): String @bar");

        // act
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var acResult = a.Equals(c);
        var aNullResult = a.Equals(default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void GetHashCode_With_Location()
    {
        // arrange
        var a = ParseFieldDefinition("foo(a: String): String @bar");
        var b = ParseFieldDefinition("   foo  (a : String): String @bar");
        var c = ParseFieldDefinition("bar(a: String): String @bar");
        var d = ParseFieldDefinition("   bar  (a : String): String @bar");

        // act
        var aHash = a.GetHashCode();
        var bHash = b.GetHashCode();
        var cHash = c.GetHashCode();
        var dHash = d.GetHashCode();

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
        Assert.NotEqual(aHash, dHash);
    }
}
