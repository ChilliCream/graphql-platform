namespace HotChocolate.Types.Directives;

public class DeprecatedDirectiveTests
{
    [Fact]
    public void Constructor_Should_SetTheDefaultReason_When_NoReasonIsGiven()
    {
        // act
        var directive = new DeprecatedDirective();

        // assert
        Assert.Equal("No longer supported", directive.Reason);
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_ReasonIsNull()
    {
        // act
        static void Action() => _ = new DeprecatedDirective(null!);

        // assert
        var exception = Assert.Throws<ArgumentNullException>(Action);
        Assert.Equal("reason", exception.ParamName);
    }

    [Fact]
    public void CreateNode_Should_NameTheReasonArgument_When_ReasonIsNotTheDefault()
    {
        // act
        var node = DeprecatedDirective.CreateNode("Use `bar` instead.");

        // assert
        node.ToString().MatchInlineSnapshot("""@deprecated(reason: "Use `bar` instead.")""");
    }

    [Fact]
    public void CreateNode_Should_OmitTheReasonArgument_When_ReasonIsTheDefault()
    {
        // act
        var node = DeprecatedDirective.CreateNode(DirectiveNames.Deprecated.Arguments.DefaultReason);

        // assert
        node.ToString().MatchInlineSnapshot("@deprecated");
    }
}
