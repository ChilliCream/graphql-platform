using HotChocolate.Language;

namespace HotChocolate.Fusion;

public sealed class FieldSelectionTests
{
    [Theory]
    [InlineData("a b", "a b")]
    [InlineData("b a", "a b")]
    [InlineData("a      b", "a b")]
    [InlineData("x { b a }", "x { a b }")]
    [InlineData("a { b } c", "a { b } c")]
    [InlineData("a { b c }", "a { b c }")]
    [InlineData("x { y { b a } } c", "c x { y { a b } }")]
    public void Normalize_Should_IgnoreWhitespaceAndOrder_When_SelectionIsValid(
        string input,
        string expected)
    {
        // act
        var result = FieldSelection.Normalize(input);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("a a b", "a b")]
    [InlineData("a { b } a { c }", "a { b c }")]
    [InlineData("a { b } a { b }", "a { b }")]
    [InlineData("... on A { b } ... on A { c }", "... on A { b c }")]
    [InlineData("... on A { b } ... on B { b }", "... on A { b } ... on B { b }")]
    [InlineData("a(y: 2, x: 1) a(x: 1, y: 2)", "a(x: 1, y: 2)")]
    [InlineData("a(x: 1) a(x: 2)", "a(x: 1) a(x: 2)")]
    public void Normalize_Should_MergeEqualSelections_When_SelectionsRepeat(
        string input,
        string expected)
    {
        // act
        var result = FieldSelection.Normalize(input);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("... { a } b", "a b")]
    [InlineData("s ... on A { ... { b } } ... on A { b }", "... on A { b } s")]
    [InlineData("s ... on A { ... on A { b } } ... on A { b }", "... on A { b } s")]
    [InlineData("... on A { ... on B { b } }", "... on A { ... on B { b } }")]
    public void Normalize_Should_InlineRedundantFragments_When_FragmentAddsNoTypeCondition(
        string input,
        string expected)
    {
        // act
        var result = FieldSelection.Normalize(input);

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_Should_ThrowSyntaxException_When_SelectionIsInvalid()
    {
        // act
        void Act() => FieldSelection.Normalize("a {");

        // assert
        Assert.Throws<SyntaxException>(Act);
    }
}
