using Xunit;
using HotChocolate.Caching;

namespace Caching.Tests;

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
    [InlineData(true)]
    [InlineData(false)]
    public void ValidInheritMaxAge(bool? inheritMaxAge)
    {
        var cacheControl = new CacheControlDirective(0,
            CacheControlScope.Private, inheritMaxAge);

        Assert.Equal(inheritMaxAge, cacheControl.InheritMaxAge);
    }
}