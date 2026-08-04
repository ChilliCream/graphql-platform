namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class TopicTemplateTests
{
    // Resolves "args.id" to "1" to match the authoritative truth table; any other expression
    // surfaces as an unsupported-expression marker so a misrouted expression is visible.
    private static readonly TopicExpressionResolver s_resolver =
        expression => expression.SequenceEqual("args.id")
            ? "1"
            : $"<unsupported:{expression.ToString()}>";

    [Fact]
    public void Expand_Should_ReturnVerbatim_When_TemplateHasNoBraces()
    {
        // arrange
        const string topic = "onUserCreated";

        // act
        var result = TopicTemplate.Expand(topic, s_resolver);

        // assert
        Assert.Equal("onUserCreated", result);
    }

    [Fact]
    public void Expand_Should_ResolvePlaceholder_When_TemplateHasSinglePlaceholder()
    {
        // arrange
        // truth table: foo-{$args.id} -> foo-1
        const string topic = "foo-{$args.id}";

        // act
        var result = TopicTemplate.Expand(topic, s_resolver);

        // assert
        Assert.Equal("foo-1", result);
    }

    [Fact]
    public void Expand_Should_EmitLiteralBraces_When_TemplateHasEscapedBraces()
    {
        // arrange
        // truth table: foo-{{lit}} -> foo-{lit}
        const string topic = "foo-{{lit}}";

        // act
        var result = TopicTemplate.Expand(topic, s_resolver);

        // assert
        Assert.Equal("foo-{lit}", result);
    }

    [Fact]
    public void Expand_Should_ResolvePlaceholderFlankedByEscapedBraces_When_TemplateNestsBraces()
    {
        // arrange
        // truth table: foo-{{{$args.id}}} -> foo-{1}
        const string topic = "foo-{{{$args.id}}}";

        // act
        var result = TopicTemplate.Expand(topic, s_resolver);

        // assert
        Assert.Equal("foo-{1}", result);
    }

    [Fact]
    public void Expand_Should_ResolvePlaceholderFlankedByDoubleEscapedBraces_When_TemplateNestsBraces()
    {
        // arrange
        // truth table: foo-{{{{{$args.id}}}}} -> foo-{{1}}
        const string topic = "foo-{{{{{$args.id}}}}}";

        // act
        var result = TopicTemplate.Expand(topic, s_resolver);

        // assert
        Assert.Equal("foo-{{1}}", result);
    }

    [Fact]
    public void Expand_Should_NotResolve_When_PlaceholderIsFullyEscaped()
    {
        // arrange
        // truth table: {{$args.id}} -> {$args.id} (fully escaped, NOT resolved)
        const string topic = "{{$args.id}}";

        // act
        var result = TopicTemplate.Expand(topic, s_resolver);

        // assert
        Assert.Equal("{$args.id}", result);
    }

    [Fact]
    public void Expand_Should_TreatTrailingDoubleBraceAsLiteral_When_PlaceholderPrecedesIt()
    {
        // arrange
        // truth table: foo-{$args.id}}} -> foo-1} (placeholder, then "}}" -> literal "}")
        const string topic = "foo-{$args.id}}}";

        // act
        var result = TopicTemplate.Expand(topic, s_resolver);

        // assert
        Assert.Equal("foo-1}", result);
    }

    [Fact]
    public void Expand_Should_Throw_When_OpeningBraceIsUnescaped()
    {
        // arrange
        // error case: foo-{bad} (unescaped "{")
        const string topic = "foo-{bad}";

        // act
        void Act() => TopicTemplate.Expand(topic, s_resolver);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(
            "The topic template `foo-{bad}` contains an unescaped `{`. "
            + "Use `{{` to write a literal brace.",
            exception.Message);
    }

    [Fact]
    public void Expand_Should_Throw_When_ClosingBraceIsUnescaped()
    {
        // arrange
        // error case: foo-} (unescaped "}")
        const string topic = "foo-}";

        // act
        void Act() => TopicTemplate.Expand(topic, s_resolver);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(
            "The topic template `foo-}` contains an unescaped `}`. "
            + "Use `}}` to write a literal brace.",
            exception.Message);
    }

    [Fact]
    public void Expand_Should_Throw_When_PlaceholderIsUnterminated()
    {
        // arrange
        // error case: foo-{$args.id (unterminated)
        const string topic = "foo-{$args.id";

        // act
        void Act() => TopicTemplate.Expand(topic, s_resolver);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(
            "The topic template `foo-{$args.id` contains an unterminated placeholder.",
            exception.Message);
    }

    [Fact]
    public void Expand_Should_EmitLiteralPlaceholderText_When_BracesAreEscaped()
    {
        // arrange
        // a literal "{$x}" is authored by escaping both braces: "{{$x}}".
        const string topic = "{{$x}}";

        // act
        var result = TopicTemplate.Expand(topic, s_resolver);

        // assert
        Assert.Equal("{$x}", result);
    }
}
