using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
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
        public void BuildRequest_OnlyQueryDocIsSet_RequestHasOnlyQuery()
        {
            // arrange
            DocumentNode query = Utf8GraphQLParser.Parse("{ foo }");

            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
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
            void Action() =>
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .Create();

            // assert
            Assert.Equal("sourceText",
                Assert.Throws<ArgumentException>(Action).ParamName);
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
        public void BuildRequest_QueryAndSetVariable_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddVariableValue("one", "foo")
                    .SetVariableValue("one", "bar")
                    .Create();

            // assert
            // one should be bar
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetNewVariable_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .SetVariableValue("one", "bar")
                    .Create();

            // assert
            // one should be bar
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
            // only three should exist
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetProperty_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddProperty("one", "foo")
                    .SetProperty("one", "bar")
                    .Create();

            // assert
            // one should be bar
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetNewProperty_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .SetProperty("one", "bar")
                    .Create();

            // assert
            // one should be bar
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

        [Fact]
        public void BuildRequest_QueryAndTryAddProperties_PropertyIsSet()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .TryAddProperty("one", "bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndTryAddProperties_PropertyIsNotSet()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddProperty("one", "foo")
                    .TryAddProperty("one", "bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndTryAddVariable_VariableIsSet()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .TryAddVariableValue("one", "bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndTryAddVariable_VariableIsNotSet()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddVariableValue("one", "foo")
                    .TryAddVariableValue("one", "bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndTryAddExtension_ExtensionIsSet()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .TryAddExtension("one", "bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndTryAddExtension_ExtensionIsNotSet()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddExtension("one", "foo")
                    .TryAddExtension("one", "bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndAddExtension_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddExtension("one", "foo")
                    .AddExtension("two", "bar")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetExtensions_RequestIsCreated_1()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddExtension("one", "foo")
                    .AddExtension("two", "bar")
                    .SetExtensions(new Dictionary<string, object>
                    {
                        { "three", "baz" }
                    })
                    .Create();

            // assert
            // only three should exist
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetExtensions_RequestIsCreated_2()
        {
            // arrange
            IReadOnlyDictionary<string, object> ext =
                new Dictionary<string, object>
                {
                    { "three", "baz" }
                };

            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddExtension("one", "foo")
                    .AddExtension("two", "bar")
                    .SetExtensions(ext)
                    .Create();

            // assert
            // only three should exist
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetExtensions_RequestIsCreated_3()
        {
            // arrange
            IReadOnlyDictionary<string, object> ext =
                new Dictionary<string, object>
                {
                    { "three", "baz" }
                };

            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddExtension("one", "foo")
                    .AddExtension("two", "bar")
                    .SetExtensions(ext)
                    .AddExtension("four", "bar")
                    .Create();

            // assert
            // only three should exist
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetExtensions_RequestIsCreated_4()
        {
            // arrange
            IDictionary<string, object> ext =
                new Dictionary<string, object>
                {
                    { "three", "baz" }
                };

            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddExtension("one", "foo")
                    .AddExtension("two", "bar")
                    .SetExtensions(ext)
                    .Create();

            // assert
            // only three should exist
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetExtensions_RequestIsCreated_5()
        {
            // arrange
            IDictionary<string, object> ext =
                new Dictionary<string, object>
                {
                    { "three", "baz" }
                };

            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddExtension("one", "foo")
                    .AddExtension("two", "bar")
                    .SetExtensions(ext)
                    .AddExtension("four", "bar")
                    .Create();

            // assert
            // only three should exist
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_QueryAndSetExtension_RequestIsCreated()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddExtension("one", "foo")
                    .SetExtension("one", "bar")
                    .Create();

            // assert
            // one should be bar
            request.MatchSnapshot();
        }
    }
}
