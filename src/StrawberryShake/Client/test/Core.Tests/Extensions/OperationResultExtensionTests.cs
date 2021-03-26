using System;
using System.Collections.Generic;
using Xunit;

namespace StrawberryShake.Extensions
{
    public class OperationResultExtensionTests
    {
        [Fact]
        public void EnsureNoErrors_WithNoErrors()
        {
            // arrange
            var successResult = new ResultMock();

            // act
            successResult.EnsureNoErrors();

            // assert
            // did not throw!
        }

        [Fact]
        public void EnsureNoErrors_WithErrors()
        {
            // arrange
            var clientError = new ClientError("test");
            var successResult = new ResultMock(clientError);

            // act
            void Throws() => successResult.EnsureNoErrors();

            // assert
            Assert.Collection(
                Assert.Throws<GraphQLClientException>(Throws).Errors,
                item => Assert.Same(clientError, item));
        }

        [Fact]
        public void EnsureNoErrors_ResultNull()
        {
            // arrange
            // act
            void Throws() => default(ResultMock)!.EnsureNoErrors();

            // assert
            Assert.Throws<ArgumentNullException>(Throws);
        }

        [Fact]
        public void HasErrors_WithNoErrors()
        {
            // arrange
            var successResult = new ResultMock();

            // act
            var hasErrors = successResult.IsErrorResult();

            // assert
            Assert.False(hasErrors!);
        }

        [Fact]
        public void HasErrors_WithErrors()
        {
            // arrange
            var clientError = new ClientError("test");
            var successResult = new ResultMock(clientError);

            // act
            var hasErrors = successResult.IsErrorResult();

            // assert
            Assert.True(hasErrors);
        }

        [Fact]
        public void HasErrors_ResultNull()
        {
            // arrange
            // act
            void Throws() => default(ResultMock)!.IsErrorResult();

            // assert
            Assert.Throws<ArgumentNullException>(Throws);
        }

        [Fact]
        public void IsSuccessResult_WithNoErrors()
        {
            // arrange
            var successResult = new ResultMock();

            // act
            var hasErrors = successResult.IsSuccessResult();

            // assert
            Assert.True(hasErrors!);
        }

        [Fact]
        public void IsSuccessResult_WithErrors()
        {
            // arrange
            var clientError = new ClientError("test");
            var successResult = new ResultMock(clientError);

            // act
            var hasErrors = successResult.IsSuccessResult();

            // assert
            Assert.False(hasErrors);
        }

        [Fact]
        public void IsSuccessResult_ResultNull()
        {
            // arrange
            // act
            void Throws() => default(ResultMock)!.IsSuccessResult();

            // assert
            Assert.Throws<ArgumentNullException>(Throws);
        }

        private class ResultMock : IOperationResult
        {
            public ResultMock()
            {
                Errors = Array.Empty<IClientError>();
            }

            public ResultMock(IClientError error)
            {
                Errors = new[] { error };
            }

            public object? Data { get; } = default!;
            public Type DataType { get; } = default!;
            public IOperationResultDataInfo? DataInfo { get; } = default!;
            public object DataFactory { get; } = default!;
            public IReadOnlyList<IClientError> Errors { get; }
            public IReadOnlyDictionary<string, object?> Extensions { get; } = default!;
            public IReadOnlyDictionary<string, object?> ContextData { get; } = default!;
        }
    }
}
