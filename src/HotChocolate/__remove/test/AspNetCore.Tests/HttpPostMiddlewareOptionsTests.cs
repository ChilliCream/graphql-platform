using System;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class HttpPostMiddlewareOptionsTests
    {
        [Fact]
        public void SetEmptyPathString()
        {
            // arrange
            var options = new HttpPostMiddlewareOptions();

            // act
            Action action = () => options.Path = default;

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void SetPath()
        {
            // arrange
            var options = new HttpPostMiddlewareOptions();

            // act
            options.Path = new PathString("/foo");

            // assert
            Assert.Equal(new PathString("/foo"), options.Path);
        }

        [Fact]
        public void SetMaxRequestSizeToZero()
        {
            // arrange
            var options = new HttpPostMiddlewareOptions();

            // act
            Action action = () => options.MaxRequestSize = 0;

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void SetMaxRequestSize()
        {
            // arrange
            var options = new HttpPostMiddlewareOptions();

            // act
            options.MaxRequestSize = 4096 * 1000;

            // assert
            Assert.Equal(4096 * 1000, options.MaxRequestSize);
        }

        [Fact]
        public void DefaultParserOptionsAreSet()
        {
            // arrange
            var options = new QueryMiddlewareOptions();

            // act
            ParserOptions parserOptions = options.ParserOptions;

            // assert
            Assert.NotNull(parserOptions);
        }

        [Fact]
        public void CannotSetParserOptionsNull()
        {
            // arrange
            var options = new QueryMiddlewareOptions();

            // act
            Action action = () => options.ParserOptions = null;

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void SetParserOptions()
        {
            // arrange
            var options = new QueryMiddlewareOptions();
            var parserOptions = new ParserOptions();

            // act
            options.ParserOptions = parserOptions;

            // assert
            Assert.Equal(parserOptions, options.ParserOptions);
        }
    }
}
