using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class FilterFlagsTests
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void Compile_Should_AndEveryConvenienceFlagAroundTheParsedTree_When_FlagsAreProvided()
    {
        // act
        var result = FilterFlags.Compile(
            "http.status_code:>=500",
            TelemetryFilterSignal.Logs,
            hasError: true,
            minDurationMs: 1000,
            severity: "warn",
            traceId: "abc",
            search: "timeout",
            service: "checkout");

        // assert
        Serialize(result).MatchInlineSnapshot(
            """
            {
              "and": [
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
                        "string": "error"
                      }
                    },
                    "key": "status"
                  }
                },
                {
                  "attribute": {
                    "condition": {
                      "gte": {
                        "int": 1000
                      }
                    },
                    "key": "duration"
                  }
                },
                {
                  "attribute": {
                    "condition": {
                      "in": [
                        {
                          "string": "warn"
                        },
                        {
                          "string": "error"
                        },
                        {
                          "string": "fatal"
                        }
                      ]
                    },
                    "key": "severity"
                  }
                },
                {
                  "attribute": {
                    "condition": {
                      "eq": {
                        "string": "abc"
                      }
                    },
                    "key": "trace.id"
                  }
                },
                {
                  "attribute": {
                    "condition": {
                      "matches": "*timeout*"
                    },
                    "key": "log.message"
                  }
                },
                {
                  "attribute": {
                    "condition": {
                      "eq": {
                        "string": "checkout"
                      }
                    },
                    "key": "service.name",
                    "kind": "Resource"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public void Compile_Should_ReturnTheParsedFilter_When_FilterIsProvided()
    {
        // act
        _ = FilterFlags.Compile(
            "http.statuscode:>=500",
            TelemetryFilterSignal.Traces,
            hasError: false,
            minDurationMs: null,
            severity: null,
            traceId: null,
            search: null,
            service: null,
            out var parsedFilter);

        // assert
        var predicate = Assert.IsType<FilterPredicateNode>(parsedFilter);
        Assert.Equal("http.statuscode", predicate.Field);
    }

    [Theory]
    [InlineData("trace", 6)]
    [InlineData("debug", 5)]
    [InlineData("info", 4)]
    [InlineData("warn", 3)]
    [InlineData("error", 2)]
    [InlineData("fatal", 1)]
    public void Compile_Should_IncludeEveryHigherSeverity_When_ASeverityFlagIsProvided(
        string severity,
        int expectedCount)
    {
        // act
        var result = FilterFlags.Compile(
            null,
            TelemetryFilterSignal.Logs,
            hasError: false,
            minDurationMs: null,
            severity,
            traceId: null,
            search: null,
            service: null);

        // assert
        Assert.Equal(expectedCount, result!.Attribute!.Condition.In!.Count);
    }

    private static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, s_serializerOptions);
}
