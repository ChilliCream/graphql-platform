using System;
using Microsoft.Owin;
using Xunit;

namespace HotChocolate.AspNetClassic
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
