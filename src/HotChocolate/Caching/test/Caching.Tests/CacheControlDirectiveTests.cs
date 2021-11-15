using Xunit;
using HotChocolate.Caching;
using System;

namespace Caching.Tests;

public class CacheControlDirectiveTests
{
    [Fact]
    public void MaxAgeBelowZero()
    {
        void Action() => new CacheControlDirective();

        Assert.Throws<ArgumentOutOfRangeException>(Action);
    }
}