using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StrawberryShake
{
    public class ResultParserCollectionTests
    {
        [Fact]
        public void Resolve_Parser()
        {
            // arrange
            var dummy = new DummyResultParser();
            var dict = new Dictionary<Type, IResultParser> { { dummy.ResultType, dummy } };
            var resolver = new ResultParserCollection(dict);

            // act
            IResultParser parser = resolver.Get(typeof(string));

            // assert
            Assert.NotNull(parser);
            Assert.IsType<DummyResultParser>(parser);
        }

        [Fact]
        public void Resolve_Parser_Not_Found()
        {
            // arrange
            var dummy = new DummyResultParser();
            var dict = new Dictionary<Type, IResultParser> { { dummy.ResultType, dummy } };
            var resolver = new ResultParserCollection(dict);

            // act
            Action action = () => resolver.Get(typeof(int));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void Resolve_Type_Is_Null()
        {
            // arrange
            var dummy = new DummyResultParser();
            var dict = new Dictionary<Type, IResultParser> { { dummy.ResultType, dummy } };
            var resolver = new ResultParserCollection(dict);

            // act
            Action action = () => resolver.Get(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Constructor_Resolvers_Are_null()
        {
            // arrange
            // act
            Action action = () => new ResultParserCollection(null);

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
