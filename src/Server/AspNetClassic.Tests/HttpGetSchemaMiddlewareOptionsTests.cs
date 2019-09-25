using System;
using Microsoft.Owin;
using Xunit;

namespace HotChocolate.AspNetClassic
{
    public class HttpGetSchemaMiddlewareOptionsTests
    {
        [Fact]
        public void SetEmptyPathString()
        {
            // arrange
            var options = new HttpGetSchemaMiddlewareOptions();

            // act
            Action action = () => options.Path = default;

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void SetPath()
        {
            // arrange
            var options = new HttpGetSchemaMiddlewareOptions();

            // act
            options.Path = new PathString("/foo");

            // assert
            Assert.Equal(new PathString("/foo"), options.Path);
        }
    }
}
