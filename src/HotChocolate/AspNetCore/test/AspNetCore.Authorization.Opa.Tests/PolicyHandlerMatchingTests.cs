using HotChocolate.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

public class PolicyHandlerMatchingTests
{
    [Fact]
    public void ThrowsWhenNoMatchingHandlerFound()
    {
        // arrange
        var options = new OpaOptions();

        // act
        ParseResult FindHandler() => options.GetPolicyResultParser("graphql/policy");

        // assert
        Assert.Throws<InvalidOperationException>(FindHandler);
    }

    [Fact]
    public void MatchesExact()
    {
        // arrange
        var options = new OpaOptions();
        var parser = new ParseResult(_ => AuthorizeResult.Allowed);
        options.PolicyResultHandlers.Add("my/policy", parser);

        // act
        var foundHandler = options.GetPolicyResultParser("my/policy");

        // assert
        Assert.Equal(parser, foundHandler);
    }

    [Fact]
    public void MatchesRegex()
    {
        // arrange
        var options = new OpaOptions();
        var parser = new ParseResult(_ => AuthorizeResult.Allowed);
        options.PolicyResultHandlers.Add("graphql\\/.*", parser);

        // act
        var foundHandler = options.GetPolicyResultParser("graphql/policy");

        // assert
        Assert.Equal(parser, foundHandler);
    }

    [Fact]
    public void ExactMatchTakesPriorityOverRegex()
    {
        // arrange
        var options = new OpaOptions();
        var regexHandler = new ParseResult(_ => AuthorizeResult.Allowed);
        options.PolicyResultHandlers.Add("graphql\\/.*", regexHandler);
        var exactHandler = new ParseResult(_ => AuthorizeResult.Allowed);
        options.PolicyResultHandlers.Add("graphql/policy", exactHandler);

        // act
        var foundHandler = options.GetPolicyResultParser("graphql/policy");

        // assert
        Assert.Equal(exactHandler, foundHandler);
    }

    [Fact]
    public void OnlySingleRegexMatchIsAllowed()
    {
        // arrange
        var options = new OpaOptions();
        var regexHandler = new ParseResult(_ => AuthorizeResult.Allowed);
        options.PolicyResultHandlers.Add("graphql\\/.*", regexHandler);
        var exactHandler = new ParseResult(_ => AuthorizeResult.Allowed);
        options.PolicyResultHandlers.Add("graphql\\/p.*", exactHandler);

        // act
        ParseResult FindHandler() => options.GetPolicyResultParser("graphql/policy");

        // assert
        Assert.Throws<InvalidOperationException>(FindHandler);
    }
}
