using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class QueryMiddlewareOptionsTests
    {
        [Fact]
        public void SetEmptyPathString()
        {
            // arrange
            var options = new QueryMiddlewareOptions();

            // act
            Action action = () => options.Path = default;

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void SetEmptySubscriptionPathString()
        {
            // arrange
            var options = new QueryMiddlewareOptions();

            // act
            Action action = () => options.SubscriptionPath = default;

            // assert
            Assert.Throws<ArgumentException>(action);
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
