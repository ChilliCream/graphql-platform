using System.Collections.Immutable;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlDirectiveTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(1000)]
    public void ValidMaxAge(int? maxAge)
    {
        var cacheControl = new CacheControlDirective(maxAge);

        Assert.Equal(maxAge, cacheControl.MaxAge);
        Assert.False(cacheControl.SharedMaxAge.HasValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(1000)]
    public void ValidSharedMaxAge(int? maxAge)
    {
        var cacheControl = new CacheControlDirective(sharedMaxAge: maxAge);

        Assert.Equal(maxAge, cacheControl.SharedMaxAge);
        Assert.False(cacheControl.MaxAge.HasValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(CacheControlScope.Private)]
    [InlineData(CacheControlScope.Public)]
    public void ValidScope(CacheControlScope? scope)
    {
        var cacheControl = new CacheControlDirective(0, scope);

        Assert.Equal(scope, cacheControl.Scope);
    }

    [Theory]
    [InlineData(null)]
    [InlineData([new string[0]])]
    [InlineData([new[] {"a", "b"}])]
    public void ValidVary(string[]? vary)
    {
        var cacheControl = new CacheControlDirective(0, vary: vary?.ToImmutableArray());

        Assert.Equal(vary, cacheControl.Vary);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidInheritMaxAge(bool? inheritMaxAge)
    {
        var cacheControl = new CacheControlDirective(0,
            CacheControlScope.Private, inheritMaxAge);

        Assert.Equal(inheritMaxAge, cacheControl.InheritMaxAge);
    }
}
