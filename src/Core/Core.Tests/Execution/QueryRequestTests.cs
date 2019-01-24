using System;
using System.Collections.Generic;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Execution
{
    public class QueryRequestTests
    {
        [Fact]
        public void CreateRequest()
        {
            // act
            var request = new QueryRequest("abc");

            // assert
            Assert.Equal("abc", request.Query);
            Assert.Null(request.InitialValue);
            Assert.Null(request.OperationName);
            Assert.Null(request.Properties);
            Assert.Null(request.Services);
            Assert.Null(request.VariableValues);
        }

        [Fact]
        public void CreateRequestWithOperation()
        {
            // act
            var request = new QueryRequest("abc", "cde");

            // assert
            Assert.Equal("abc", request.Query);
            Assert.Equal("cde", request.OperationName);
            Assert.Null(request.InitialValue);
            Assert.Null(request.Properties);
            Assert.Null(request.Services);
            Assert.Null(request.VariableValues);
        }

        [Fact]
        public void FromReadOnly()
        {
            // arrange
            var original = new QueryRequest("abc", "def")
            {
                InitialValue = "ghi",
                Properties = new Dictionary<string, object>
                {
                    { "foo", null }
                },
                VariableValues = new Dictionary<string, object>
                {
                    { "bar", null }
                },
                Services = new EmptyServiceProvider()
            };

            // act
            var copy = new QueryRequest(original.ToReadOnly());

            // assert
            Assert.Equal("abc", copy.Query);
            Assert.Equal("def", copy.OperationName);
            Assert.Equal("ghi", copy.InitialValue);
            Assert.Collection(copy.Properties,
                t => Assert.Equal("foo", t.Key));
            Assert.Collection(copy.VariableValues,
                t => Assert.Equal("bar", t.Key));
            Assert.Equal(original.Services, copy.Services);
        }

        [Fact]
        public void CreateRequest_QueryIsNull()
        {
            // act
            Action a = () => new QueryRequest(default(string));
            Action b = () => new QueryRequest(default(string), "foo");

            // assert
            Assert.Equal("query",
                Assert.Throws<ArgumentException>(a).ParamName);
            Assert.Equal("query",
                Assert.Throws<ArgumentException>(b).ParamName);
        }

        [Fact]
        public void CreateRequest_QueryIsEmpty()
        {
            // act
            Action a = () => new QueryRequest(string.Empty);
            Action b = () => new QueryRequest(string.Empty, "foo");

            // assert
            Assert.Equal("query",
                Assert.Throws<ArgumentException>(a).ParamName);
            Assert.Equal("query",
                Assert.Throws<ArgumentException>(b).ParamName);
        }
    }
}
