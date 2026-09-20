using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class FilterCompilerTests
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void Compile_Should_CompileTheWorkedPortalExample_When_ItUsesAndOrAndSetSugar()
    {
        // arrange
        const string filter =
            "(http.status_code:>=500 OR http.status_code:0) AND service.name:(\"a\" OR \"b\")";

        // act
        var result = FilterCompiler.Compile(filter, "span.name", TelemetryFilterSignal.Traces);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """
            {
              "and": [
                {
                  "or": [
                    {
                      "attribute": {
                        "condition": {
                          "gte": {
                            "int": 500
                          }
                        },
                        "key": "http.status_code"
                      }
                    },
                    {
                      "attribute": {
                        "condition": {
                          "eq": {
                            "int": 0
                          }
                        },
                        "key": "http.status_code"
                      }
                    }
                  ]
                },
                {
                  "attribute": {
                    "condition": {
                      "in": [
                        {
                          "string": "a"
                        },
                        {
                          "string": "b"
                        }
                      ]
                    },
                    "key": "service.name"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public void Compile_Should_LowerRangesAndNegatedExists_When_ThePortalSugarIsUsed()
    {
        // arrange
        const string filter = "duration:RANGE(-9, -1) -@event.exception.type:*";

        // act
        var result = FilterCompiler.Compile(filter, "span.name", TelemetryFilterSignal.Traces);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """
            {
              "and": [
                {
                  "and": [
                    {
                      "attribute": {
                        "condition": {
                          "gte": {
                            "int": -9
                          }
                        },
                        "key": "duration"
                      }
                    },
                    {
                      "attribute": {
                        "condition": {
                          "lte": {
                            "int": -1
                          }
                        },
                        "key": "duration"
                      }
                    }
                  ]
                },
                {
                  "attribute": {
                    "condition": {
                      "exists": false
                    },
                    "key": "exception.type",
                    "kind": "Event"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public void Compile_Should_SplitWildcardSetMembersAndLowerScope_When_ASetContainsPatterns()
    {
        // arrange
        const string filter = "@span.db.name:IN(a*, b)";

        // act
        var result = FilterCompiler.Compile(filter, "span.name", TelemetryFilterSignal.Traces);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """
            {
              "or": [
                {
                  "attribute": {
                    "condition": {
                      "in": [
                        {
                          "string": "b"
                        }
                      ]
                    },
                    "key": "db.name",
                    "kind": "Span"
                  }
                },
                {
                  "attribute": {
                    "condition": {
                      "matches": "a*"
                    },
                    "key": "db.name",
                    "kind": "Span"
                  }
                }
              ]
            }
            """);
    }

    [Theory]
    [InlineData("timeout", "span.name", TelemetryFilterSignal.Traces, "*timeout*")]
    [InlineData("timeout", "log.message", TelemetryFilterSignal.Logs, "*timeout*")]
    [InlineData("\\@span", "span.name", TelemetryFilterSignal.Traces, "*@span*")]
    [InlineData("*HTTP*", "log.message", TelemetryFilterSignal.Logs, "*HTTP*")]
    public void Compile_Should_UseTheSuppliedFreeTextKey_When_TheInputIsATerm(
        string filter,
        string freeTextKey,
        TelemetryFilterSignal signal,
        string expectedPattern)
    {
        // act
        var result = FilterCompiler.Compile(filter, freeTextKey, signal);

        // assert
        Assert.Equal(expectedPattern, result!.Attribute!.Condition.Matches);
        Assert.Equal(freeTextKey, result.Attribute.Key);
    }

    [Theory]
    [InlineData("status:error status:ok", TelemetryFilterSignal.Traces)]
    [InlineData("severity:fatal severity:error severity:warn severity:info severity:debug", TelemetryFilterSignal.Logs)]
    [InlineData("service.name:\"api-gateway\" http.status_code:>=500 env:prod", TelemetryFilterSignal.Traces)]
    [InlineData("-service.version:\"1.0.0\" duration:>1000", TelemetryFilterSignal.Traces)]
    [InlineData("http.status_code:IN(500, 502, 503)", TelemetryFilterSignal.Traces)]
    [InlineData("http.url:*/api/*", TelemetryFilterSignal.Traces)]
    [InlineData("-http.status_code:404", TelemetryFilterSignal.Traces)]
    [InlineData("graphql.operation.name:(*ProductById OR test*)", TelemetryFilterSignal.Traces)]
    [InlineData("-(status:ok OR status:unset)", TelemetryFilterSignal.Traces)]
    [InlineData("@span.duration:>5", TelemetryFilterSignal.Traces)]
    [InlineData("@event.exception.type:\"TimeoutError\"", TelemetryFilterSignal.Traces)]
    [InlineData("@log.OriginalFormat:*timeout*", TelemetryFilterSignal.Logs)]
    [InlineData("@body.message:hello", TelemetryFilterSignal.Logs)]
    [InlineData("@resource.service.name:\"api\"", TelemetryFilterSignal.Traces)]
    public void Compile_Should_ProduceAnInputTree_When_ASectionElevenPortalExampleIsProvided(
        string filter,
        TelemetryFilterSignal signal)
    {
        // act
        var result = FilterCompiler.Compile(
            filter,
            signal == TelemetryFilterSignal.Traces ? "span.name" : "log.message",
            signal);

        // assert
        AssertCompilesToAValidInputTree(result);
    }

    [Theory]
    [InlineData("duration:2.0", "float")]
    [InlineData("x:12345678901", "float")]
    [InlineData("flag:true", "boolean")]
    [InlineData("field:foo\\ bar", "string")]
    [InlineData("name:foo*", "matches")]
    [InlineData("name:foo\\*", "eq")]
    public void Compile_Should_PreservePortalScalarAndEscapeSemantics_When_ValuesUseSpecialForms(
        string filter,
        string expectedCondition)
    {
        // act
        var result = FilterCompiler.Compile(filter, "span.name", TelemetryFilterSignal.Traces);
        var condition = result!.Attribute!.Condition;

        // assert
        var serialized = Serialize(condition);
        Assert.Contains($"\"{expectedCondition}\"", serialized, StringComparison.Ordinal);
    }

    private static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, s_serializerOptions);

    private static void AssertCompilesToAValidInputTree(OpenTelemetryFilterInput? input)
    {
        Assert.NotNull(input);
        var branches = new[]
        {
            input.And is not null,
            input.Or is not null,
            input.Not is not null,
            input.Attribute is not null
        };
        Assert.Equal(1, branches.Count(static branch => branch));
    }
}
