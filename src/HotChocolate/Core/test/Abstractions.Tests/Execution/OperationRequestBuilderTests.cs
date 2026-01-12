using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using CookieCrumble.Formatters;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

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
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
            Assert.ThrowsAny<ArgumentException>(Action).ParamName);
    }

    [Fact]
    public void BuildRequest_QueryAndSetNewVariable_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetVariableValues(new Dictionary<string, object?> { ["one"] = "bar" })
                .Build();

        // assert
        // one should be bar
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
    }

    [Fact]
    public void BuildRequest_QueryAndResetVariables_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetVariableValues(new Dictionary<string, object?> { ["one"] = "bar" })
                .SetVariableValues(default(JsonDocument))
                .Build();

        // assert
        // no variable should be in the request
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
                        { "three", "baz" }
                    })
                .Build();

        // assert
        // only three should exist
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
    }

    [Fact]
    public void BuildRequest_QueryAndInitialValue_RequestIsCreated()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetGlobalState(WellKnownContextData.InitialValue, new { a = "123" })
                .Build();

        // assert
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
    }

    [Fact]
    public void BuildRequest_QueryAndServices_RequestIsCreated()
    {
        // arrange
        var service = new { a = "123" };

        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetServices(
                    new ServiceCollection()
                        .AddSingleton(service.GetType(), service)
                        .BuildServiceProvider())
                .Build();

        // assert
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
    }

    [Fact]
    public void BuildRequest_SetAll_RequestIsCreated()
    {
        // arrange
        var service = new { a = "123" };

        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetOperationName("bar")
                .AddGlobalState("one", "foo")
                .SetVariableValues(new Dictionary<string, object?> { { "two", "bar" } })
                .SetServices(new ServiceCollection().AddSingleton(service.GetType(), service).BuildServiceProvider())
                .Build();

        // assert
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
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
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
    }

    [Fact]
    public void BuildRequest_SetErrorHandlingMode()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetErrorHandlingMode(ErrorHandlingMode.Halt)
                .Build();

        // assert
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
    }

    [Fact]
    public void BuildRequest_SetErrorHandlingMode_VariableBatchRequest()
    {
        // arrange
        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .SetErrorHandlingMode(ErrorHandlingMode.Halt)
                .SetVariableValues([new Dictionary<string, object?> { ["one"] = "foo" }])
                .Build();

        // assert
        request.MatchSnapshot(formatter: OperationRequestSnapshotFormatter.Instance);
    }
}

public class OperationRequestSnapshotFormatter : SnapshotValueFormatter<IOperationRequest>
{
    public static OperationRequestSnapshotFormatter Instance { get; } = new OperationRequestSnapshotFormatter();

    protected override void Format(IBufferWriter<byte> snapshot, IOperationRequest value)
    {
        var jsonOptions = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var jsonWriter = new Utf8JsonWriter(snapshot, jsonOptions);

        switch (value)
        {
            case VariableBatchRequest vr:
                WriteVariableBatchRequest(jsonWriter, vr);
                break;

            case OperationRequest or:
                WriteOperationRequest(jsonWriter, or);
                break;

            default:
                throw new NotSupportedException();
        }

        jsonWriter.Flush();
    }

    private static void WriteOperationRequest(Utf8JsonWriter writer, OperationRequest request)
    {
        writer.WriteStartObject();

        WriteCommonProperties(writer, request);

        if (request.VariableValues is not null)
        {
            writer.WritePropertyName("variableValues");
            request.VariableValues.Document.RootElement.WriteTo(writer);
        }

        writer.WriteEndObject();
    }

    private static void WriteVariableBatchRequest(Utf8JsonWriter writer, VariableBatchRequest request)
    {
        writer.WriteStartObject();

        WriteCommonProperties(writer, request);

        writer.WritePropertyName("variableValues");
        request.VariableValues.Document.RootElement.WriteTo(writer);

        writer.WriteEndObject();
    }

    private static void WriteCommonProperties(Utf8JsonWriter writer, IOperationRequest request)
    {
        if (request.Document is not null)
        {
            writer.WriteString("document", request.Document.ToString());
        }

        if (request.DocumentId.HasValue)
        {
            writer.WriteString("documentId", request.DocumentId.Value);
        }

        if (!request.DocumentHash.IsEmpty)
        {
            writer.WriteStartObject("documentHash");
            writer.WriteString("algorithm", request.DocumentHash.AlgorithmName);
            writer.WriteString("value", request.DocumentHash.Value);
            writer.WriteEndObject();
        }

        if (request.OperationName is not null)
        {
            writer.WriteString("operationName", request.OperationName);
        }

        if (request.ErrorHandlingMode is not null)
        {
            writer.WriteString("errorHandlingMode", request.ErrorHandlingMode.Value.ToString());
        }

        if (request.Extensions is not null)
        {
            writer.WritePropertyName("extensions");
            request.Extensions.Document.RootElement.WriteTo(writer);
        }

        if (request.ContextData?.Count > 0)
        {
            writer.WriteStartObject("contextData");
            foreach (var kvp in request.ContextData)
            {
                writer.WritePropertyName(kvp.Key);
                WriteValue(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        if (request.Services is not null)
        {
            writer.WriteBoolean("hasServices", true);
        }

        if (request.Flags != RequestFlags.AllowAll)
        {
            writer.WriteString("flags", request.Flags.ToString());
        }
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case decimal dec:
                writer.WriteNumberValue(dec);
                break;
            default:
                writer.WriteStringValue($"[{value.GetType().Name}]");
                break;
        }
    }
}
