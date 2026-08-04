namespace HotChocolate.Types.Directives;

public class DeprecatedDirectiveTests
{
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
        var node = DeprecatedDirective.CreateNode("No longer supported.");

        // assert
        node.ToString().MatchInlineSnapshot("@deprecated");
    }
}
