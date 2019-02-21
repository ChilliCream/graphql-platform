using System;
using System.Collections.Generic;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Tests
{
    public class QueryRequestBuilderTests
    {
        [Fact]
        public void BuildRequest_OnlyQueryIsSet_RequestHasOnlyQuery()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_Empty_QueryRequestBuilderException()
        {
            // arrange
            // act
            Action action = () =>
                QueryRequestBuilder.New()
                    .Create();

            // assert
            Assert.Throws<QueryRequestBuilderException>(action)
                .Message.MatchSnapshot();
        }

        [InlineData("")]
        [InlineData(null)]
        [Theory]
        public void SetQuery_NullOrEmpty_ArgumentException(string query)
        {
            // arrange
            // act
            Action action = () =>
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .Create();

            // assert
            Assert.Equal("query",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void BuildRequest_QueryAndAddVariables_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddVariableValue("one", "foo")
                    .AddVariableValue("two", "bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetVariables_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddVariableValue("one", "foo")
                    .AddVariableValue("two", "bar")
                    .SetVariableValues(new Dictionary<string, object>
                    {
                        { "three", "baz" }
                    })
                    .Create();

            // assert
            // only three should be in the request
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndResetVariables_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddVariableValue("one", "foo")
                    .AddVariableValue("two", "bar")
                    .SetVariableValues(null)
                    .Create();

            // assert
            // no variable should be in the request
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndAddProperties_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddProperty("one", "foo")
                    .AddProperty("two", "bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetProperties_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddProperty("one", "foo")
                    .AddProperty("two", "bar")
                    .SetProperties(new Dictionary<string, object>
                    {
                        { "three", "baz" }
                    })
                    .Create();

            // assert
            // only three should be in the request
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndResetProperties_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddProperty("one", "foo")
                    .AddProperty("two", "bar")
                    .SetProperties(null)
                    .Create();

            // assert
            // no property should be in the request
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndInitialValue_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .SetInitialValue(new { a = "123" })
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndOperation_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .SetOperation("bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndResetOperation_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .SetOperation("bar")
                    .SetOperation(null)
                    .Create();

            // assert
            // the operation should be null
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndServices_RequestIsCreated()
        {
            // arrange
            var service = new { a = "123" };

            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .SetServices(new DictionaryServiceProvider(
                        service.GetType(), service))
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_SetAll_RequestIsCreated()
        {
            // arrange
            var service = new { a = "123" };

            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .SetOperation("bar")
                    .SetInitialValue(new { a = "456" })
                    .AddProperty("one", "foo")
                    .AddVariableValue("two", "bar")
                    .SetServices(new DictionaryServiceProvider(
                        service.GetType(), service))
                    .Create();

            // assert
            request.MatchSnapshot();
        }
    }
}
