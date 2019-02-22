using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching
{
    public class RemoteRemoteQueryRequestBuilderTests
    {
        [Fact]
        public void BuildRequest_OnlyQueryIsSet_RequestHasOnlyQuery()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse("{ foo }");

            // act
            IRemoteQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
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
                RemoteQueryRequestBuilder.New()
                    .Create();

            // assert
            Assert.Throws<QueryRequestBuilderException>(action)
                .Message.MatchSnapshot();
        }

        [Fact]
        public void SetQuery_Null_ArgumentException()
        {
            // arrange
            // act
            Action action = () =>
                RemoteQueryRequestBuilder.New()
                    .SetQuery(null)
                    .Create();

            // assert
            Assert.Equal("query",
                Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public void BuildRequest_QueryAndAddVariables_RequestIsCreated()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse("{ foo }");

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
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
            DocumentNode query = Parser.Default.Parse("{ foo }");

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
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
            DocumentNode query = Parser.Default.Parse("{ foo }");

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
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
            DocumentNode query = Parser.Default.Parse("{ foo }");

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
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
            DocumentNode query = Parser.Default.Parse("{ foo }");

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
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
            DocumentNode query = Parser.Default.Parse("{ foo }");

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
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
            DocumentNode query = Parser.Default.Parse("{ foo }");

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetInitialValue(new { a = "123" })
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndOperation_RequestIsCreated()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse("query bar { foo }");

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetOperation("bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_InvalidOperation_QueryRequestBuilderException()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse("query baz { foo }");

            // act
            Action action = () =>
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetOperation("bar")
                    .Create();

            // assert
            Assert.Equal(
                "Specify an operation name in order to create " +
                "a query request that contains multiple operations.",
                Assert.Throws<QueryRequestBuilderException>(action).Message);
        }

        [Fact]
        public void BuildRequest_NoOperation_QueryRequestBuilderException()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(
                "query baz { foo } query qux { foo }");

            // act
            Action action = () =>
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
                    .Create();

            // assert
            Assert.Equal(
                "Specify an operation name in order to create " +
                "a query request that contains multiple operations.",
                Assert.Throws<QueryRequestBuilderException>(action).Message);
        }

        [Fact]
        public void BuildRequest_QueryAndResetOperation_RequestIsCreated()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse("{ foo }");

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
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
            DocumentNode query = Parser.Default.Parse("{ foo }");
            var service = new { a = "123" };

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
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
            DocumentNode query = Parser.Default.Parse("{ foo }");
            var service = new { a = "123" };

            // act
            IReadOnlyQueryRequest request =
                RemoteQueryRequestBuilder.New()
                    .SetQuery(query)
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
