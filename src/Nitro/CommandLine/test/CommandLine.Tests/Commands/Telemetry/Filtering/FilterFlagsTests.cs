using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

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

    [Theory]
    [InlineData(null, false, null, null, null, null, null, TelemetryFilterSignal.Traces, "null")]
    [InlineData("   ", false, null, null, null, null, null, TelemetryFilterSignal.Traces, "null")]
    [InlineData(null, true, null, null, null, null, null, TelemetryFilterSignal.Traces, "attribute(status,eq(string:error))")]
    [InlineData(null, false, 250, null, null, null, null, TelemetryFilterSignal.Traces, "attribute(duration,gte(int:250))")]
    [InlineData(null, false, null, "error", null, null, null, TelemetryFilterSignal.Logs, "attribute(severity,in(string:error,string:fatal))")]
    [InlineData(null, false, null, "TRACE", null, null, null, TelemetryFilterSignal.Logs, "attribute(severity,in(string:trace,string:debug,string:info,string:warn,string:error,string:fatal))")]
    [InlineData(null, false, null, null, "abc", null, null, TelemetryFilterSignal.Traces, "attribute(trace.id,eq(string:abc))")]
    [InlineData(null, false, null, null, null, "timeout", null, TelemetryFilterSignal.Traces, "attribute(span.name,matches(*timeout*))")]
    [InlineData(null, false, null, null, null, "timeout", null, TelemetryFilterSignal.Logs, "attribute(log.message,matches(*timeout*))")]
    [InlineData(null, false, null, null, null, null, "checkout", TelemetryFilterSignal.Traces, "attribute(service.name@Resource,eq(string:checkout))")]
    [InlineData("a:1", false, null, null, null, null, null, TelemetryFilterSignal.Traces, "attribute(a,eq(int:1))")]
    [InlineData("timeout", false, null, null, null, null, null, TelemetryFilterSignal.Logs, "attribute(log.message,matches(*timeout*))")]
    public void Compile_Should_EmitOneClause_When_ASingleFlagIsSet(
        string? filter,
        bool hasError,
        int? minDurationMs,
        string? severity,
        string? traceId,
        string? search,
        string? service,
        TelemetryFilterSignal signal,
        string expected)
    {
        // act
        var result = FilterFlags.Compile(
            filter,
            signal,
            hasError,
            minDurationMs,
            severity,
            traceId,
            search,
            service);

        // assert
        Assert.Equal(expected, Describe(result));
    }

    [Theory]
    [InlineData(null, true, null, null, null, null, "checkout", TelemetryFilterSignal.Traces, "and(attribute(status,eq(string:error)),attribute(service.name@Resource,eq(string:checkout)))")]
    [InlineData(null, false, 250, null, null, "timeout", null, TelemetryFilterSignal.Traces, "and(attribute(duration,gte(int:250)),attribute(span.name,matches(*timeout*)))")]
    public void Compile_Should_AndTheClauses_When_TwoFlagsAreSet(
        string? filter,
        bool hasError,
        int? minDurationMs,
        string? severity,
        string? traceId,
        string? search,
        string? service,
        TelemetryFilterSignal signal,
        string expected)
    {
        // act
        var result = FilterFlags.Compile(
            filter,
            signal,
            hasError,
            minDurationMs,
            severity,
            traceId,
            search,
            service);

        // assert
        Assert.Equal(expected, Describe(result));
    }

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
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Compile_Should_ReturnNoParsedFilter_When_TheFilterIsNullOrWhitespace(string? filter)
    {
        // act
        var result = FilterFlags.Compile(
            filter,
            TelemetryFilterSignal.Traces,
            hasError: false,
            minDurationMs: null,
            severity: null,
            traceId: null,
            search: null,
            service: null,
            out var parsedFilter);

        // assert
        Assert.Null(parsedFilter);
        Assert.Null(result);
    }

    [Fact]
    public void Compile_Should_Throw_When_TheSeverityIsUnsupported()
    {
        // act
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => FilterFlags.Compile(
            null,
            TelemetryFilterSignal.Logs,
            hasError: false,
            minDurationMs: null,
            severity: "verbose",
            traceId: null,
            search: null,
            service: null));

        // assert
        Assert.Equal(
            $"Unsupported severity level. (Parameter 'severity'){Environment.NewLine}Actual value was verbose.",
            error.Message);
    }

    [Theory]
    [InlineData("trace", "attribute(severity,in(string:trace,string:debug,string:info,string:warn,string:error,string:fatal))")]
    [InlineData("debug", "attribute(severity,in(string:debug,string:info,string:warn,string:error,string:fatal))")]
    [InlineData("info", "attribute(severity,in(string:info,string:warn,string:error,string:fatal))")]
    [InlineData("warn", "attribute(severity,in(string:warn,string:error,string:fatal))")]
    [InlineData("error", "attribute(severity,in(string:error,string:fatal))")]
    [InlineData("fatal", "attribute(severity,in(string:fatal))")]
    public void Compile_Should_IncludeTheRequestedLevelAndEveryHigherLevel_When_ASeverityIsGiven(
        string severity,
        string expected)
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
        Assert.Equal(expected, Describe(result));
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

    private static string Describe(OpenTelemetryFilterInput? input)
    {
        if (input is null)
        {
            return "null";
        }

        if (input.And is not null)
        {
            return $"and({string.Join(',', input.And.Select(Describe))})";
        }

        if (input.Or is not null)
        {
            return $"or({string.Join(',', input.Or.Select(Describe))})";
        }

        if (input.Not is not null)
        {
            return $"not({Describe(input.Not)})";
        }

        if (input.Attribute is not null)
        {
            var kind = input.Attribute.Kind is null ? string.Empty : $"@{input.Attribute.Kind}";
            return $"attribute({input.Attribute.Key}{kind},{Describe(input.Attribute.Condition)})";
        }

        return "invalid";
    }

    private static string Describe(OpenTelemetryAttributeConditionInput condition)
    {
        if (condition.Eq is not null)
        {
            return $"eq({Describe(condition.Eq)})";
        }

        if (condition.Gt is not null)
        {
            return $"gt({Describe(condition.Gt)})";
        }

        if (condition.Gte is not null)
        {
            return $"gte({Describe(condition.Gte)})";
        }

        if (condition.Lt is not null)
        {
            return $"lt({Describe(condition.Lt)})";
        }

        if (condition.Lte is not null)
        {
            return $"lte({Describe(condition.Lte)})";
        }

        if (condition.In is not null)
        {
            return $"in({string.Join(',', condition.In.Select(Describe))})";
        }

        if (condition.Matches is not null)
        {
            return $"matches({condition.Matches})";
        }

        return $"exists({condition.Exists})";
    }

    private static string Describe(OpenTelemetryAttributeValueInput value)
    {
        if (value.String is not null)
        {
            return $"string:{value.String}";
        }

        if (value.Int is not null)
        {
            return $"int:{value.Int}";
        }

        if (value.Float is not null)
        {
            return $"float:{value.Float:0.################}";
        }

        return $"boolean:{value.Boolean!.Value.ToString().ToLowerInvariant()}";
    }
}
