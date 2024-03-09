using System;
using System.Collections.Generic;
using Snapshooter.Xunit;

namespace HotChocolate.Execution;

public class OperationResultBuilderTests
{
    [Fact]
    public void Create_Result_Without_Data_And_Errors()
    {
        // arrange
        // act
        Action result = () => OperationResultBuilder.New().Build();

        // assert
        Assert.Throws<ArgumentException>(result);
    }

    [Fact]
    public void Create_Result_Set_Data()
    {
        // arrange
        var builder = new OperationResultBuilder();

        // act
        builder.SetData(new Dictionary<string, object> { { "a", "b" }, });

        // assert
        builder.Build().MatchSnapshot();
    }

    [Fact]
    public void Create_Result_Set_Items()
    {
        // arrange
        var builder = new OperationResultBuilder();

        // act
        builder.SetItems(new List<object> { 1, });

        // assert
        builder.Build().MatchSnapshot();
    }

    [Fact]
    public void ExpectQueryResult()
    {
        // arrange
        IExecutionResult result = OperationResultBuilder.New()
            .SetData(new Dictionary<string, object> { { "a", "b" }, })
            .Build();

        // act
        var queryResult = result.ExpectQueryResult();

        // assert
        Assert.NotNull(queryResult);
    }

    [Fact]
    public void ExpectResponseStream()
    {
        // arrange
        IExecutionResult result = OperationResultBuilder.New()
            .SetData(new Dictionary<string, object> { { "a", "b" }, })
            .Build();

        // act
        void Fail() => result.ExpectResponseStream();

        // assert
        Assert.Throws<ArgumentException>(Fail);
    }
}
