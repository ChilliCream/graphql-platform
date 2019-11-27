using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StrawberryShake
{
    public class ResultParserResolverTests
    {
        [Fact]
        public void Resolve_Parser()
        {
            // arrange
            var resolver = new ResultParserResolver(new[] { new DummyResultParser() });

            // act
            IResultParser parser = resolver.GetResultParser(typeof(string));

            // assert
            Assert.NotNull(parser);
            Assert.IsType<DummyResultParser>(parser);
        }

        [Fact]
        public void Resolve_Parser_Not_Found()
        {
            // arrange
            var resolver = new ResultParserResolver(new[] { new DummyResultParser() });

            // act
            Action action = () => resolver.GetResultParser(typeof(int));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void Resolve_Type_Is_Null()
        {
            // arrange
            var resolver = new ResultParserResolver(new[] { new DummyResultParser() });

            // act
            Action action = () => resolver.GetResultParser(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Constructor_Resolvers_Are_null()
        {
            // arrange
            // act
            Action action = () => new ResultParserResolver(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        public class DummyResultParser
            : IResultParser
        {
            public Type ResultType => typeof(string);

            public Task ParseAsync(
                Stream stream,
                IOperationResultBuilder resultBuilder,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public void Parse(
                ReadOnlySpan<byte> result,
                IOperationResultBuilder resultBuilder)
            {
            }
        }
    }
}
