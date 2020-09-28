using System;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class HttpGetMiddlewareOptionsTests
    {
        [Fact]
        public void SetEmptyPathString()
        {
            // arrange
            var options = new HttpGetMiddlewareOptions();

            // act
            Action action = () => options.Path = default;

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void SetPath()
        {
            // arrange
            var options = new HttpGetMiddlewareOptions();

            // act
            options.Path = new PathString("/foo");

            // assert
            Assert.Equal(new PathString("/foo"), options.Path);
        }
    }
}
