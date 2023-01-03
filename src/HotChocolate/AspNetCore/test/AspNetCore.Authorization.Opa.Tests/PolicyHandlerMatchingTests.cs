using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Xunit;

namespace HotChocolate.AspNetCore.Authorization;

public class PolicyHandlerMatchingTests
{
    private class DummyHandler : IPolicyResultHandler
    {
        public Task<AuthorizeResult> HandleAsync(
            string policyPath,
            HttpResponseMessage response,
            IMiddlewareContext context)
            => Task.FromResult(AuthorizeResult.Allowed);
    }

    [Fact]
    public void ThrowsWhenNoMatchingHandlerFound()
    {
        // arrange
        var options = new OpaOptions();

        // act
        IPolicyResultHandler FindHandler() => options.GetResultHandlerFor("graphql/policy");

        // assert
        Assert.Throws<InvalidOperationException>((Func<IPolicyResultHandler>)FindHandler);
    }

    [Fact]
    public void MatchesExact()
    {
        // arrange
        var options = new OpaOptions();
        var handler = new DummyHandler();
        options.PolicyResultHandlers.Add("my/policy", handler);

        // act
        var foundHandler = options.GetResultHandlerFor("my/policy");

        // assert
        Assert.Equal(handler, foundHandler);
    }

    [Fact]
    public void MatchesRegex()
    {
        // arrange
        var options = new OpaOptions();
        var handler = new DummyHandler();
        options.PolicyResultHandlers.Add("graphql\\/.*", handler);

        // act
        var foundHandler = options.GetResultHandlerFor("graphql/policy");

        // assert
        Assert.Equal(handler, foundHandler);
    }

    [Fact]
    public void ExactMatchTakesPriorityOverRegex()
    {
        // arrange
        var options = new OpaOptions();
        var regexHandler = new DummyHandler();
        options.PolicyResultHandlers.Add("graphql\\/.*", regexHandler);
        var exactHandler = new DummyHandler();
        options.PolicyResultHandlers.Add("graphql/policy", exactHandler);

        // act
        var foundHandler = options.GetResultHandlerFor("graphql/policy");

        // assert
        Assert.Equal(exactHandler, foundHandler);
    }


    [Fact]
    public void OnlySingleRegexMatchIsAllowed()
    {
        // arrange
        var options = new OpaOptions();
        var regexHandler = new DummyHandler();
        options.PolicyResultHandlers.Add("graphql\\/.*", regexHandler);
        var exactHandler = new DummyHandler();
        options.PolicyResultHandlers.Add("graphql\\/p.*", exactHandler);

        // act
        IPolicyResultHandler FindHandler() => options.GetResultHandlerFor("graphql/policy");

        // assert
        Assert.Throws<InvalidOperationException>((Func<IPolicyResultHandler>)FindHandler);
    }
}
