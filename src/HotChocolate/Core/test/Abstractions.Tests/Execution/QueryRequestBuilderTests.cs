using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Execution;

public class OperationRequestBuilderTests
{
    [Fact]
    public void BuildRequest_OnlyQueryIsSet_RequestHasOnlyQuery()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .Build();

        // assert
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_OnlyQueryDocIsSet_RequestHasOnlyQuery()
    {
        // arrange
        var query = Utf8GraphQLParser.Parse("{ foo }");

        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument(query)
                .Build();

        // assert
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_Empty_OperationRequestBuilderException()
    {
        // arrange
        // act
        Action action = () =>
            OperationRequestBuilder.New()
                .Build();

        // assert
        Assert.Throws<InvalidOperationException>(action).Message.MatchSnapshot();
    }

    [InlineData("")]
    [InlineData(null)]
    [Theory]
    public void SetQuery_NullOrEmpty_ArgumentException(string? query)
    {
        // arrange
        // act
        void Action() =>
            OperationRequestBuilder.New()
                .SetDocument(query!)
                .Build();

        // assert
        Assert.Equal(
            "sourceText",
            Assert.Throws<ArgumentException>(Action).ParamName);
    }

    [Fact]
    public void BuildRequest_QueryAndSetNewVariable_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetVariableValues(new Dictionary<string, object?> { ["one"] = "bar", })
                .Build();

        // assert
        // one should be bar
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndResetVariables_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetVariableValues(new Dictionary<string, object?> { ["one"] = "bar", })
                .SetVariableValues(null)
                .Build();

        // assert
        // no variable should be in the request
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndAddProperties_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .AddGlobalState("one", "foo")
                .AddGlobalState("two", "bar")
                .Build();

        // assert
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndSetProperties_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .AddGlobalState("one", "foo")
                .AddGlobalState("two", "bar")
                .SetGlobalState(
                    new Dictionary<string, object?>
                    {
                        { "three", "baz" },
                    })
                .Build();

        // assert
        // only three should exist
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndSetProperty_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .AddGlobalState("one", "foo")
                .SetGlobalState("one", "bar")
                .Build();

        // assert
        // one should be bar
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndSetNewProperty_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetGlobalState("one", "bar")
                .Build();

        // assert
        // one should be bar
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndResetProperties_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .AddGlobalState("one", "foo")
                .AddGlobalState("two", "bar")
                .SetGlobalState(null)
                .Build();

        // assert
        // no property should be in the request
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndInitialValue_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetGlobalState(WellKnownContextData.InitialValue, new { a = "123", })
                .Build();

        // assert
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndOperation_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetOperationName("bar")
                .Build();

        // assert
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndResetOperation_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetOperationName("bar")
                .SetOperationName(null)
                .Build();

        // assert
        // the operation should be null
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndServices_RequestIsCreated()
    {
        // arrange
        var service = new { a = "123", };

        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetServices(
                    new DictionaryServiceProvider(
                        service.GetType(),
                        service))
                .Build();

        // assert
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_SetAll_RequestIsCreated()
    {
        // arrange
        var service = new { a = "123", };

        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetOperationName("bar")
                .AddGlobalState("one", "foo")
                .SetVariableValues(new Dictionary<string, object?> { { "two", "bar" }, })
                .SetServices(new DictionaryServiceProvider(service.GetType(), service))
                .Build();

        // assert
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndTryAddProperties_PropertyIsSet()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .TryAddGlobalState("one", "bar")
                .Build();

        // assert
        request.MatchSnapshot();
    }

    [Fact]
    public void BuildRequest_QueryAndTryAddProperties_PropertyIsNotSet()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .AddGlobalState("one", "foo")
                .TryAddGlobalState("one", "bar")
                .Build();

        // assert
        request.MatchSnapshot();
    }
}
