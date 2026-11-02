using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class CompiledTelemetryFilterTests
{
    [Theory]
    [InlineData(true, null, null, null, "attribute(status,eq(string:error))")]
    [InlineData(false, 250, null, null, "attribute(duration,gte(int:250))")]
    [InlineData(false, null, "timeout", null, "attribute(span.name,matches(*timeout*))")]
    [InlineData(false, null, null, "checkout", "attribute(service.name@Resource,eq(string:checkout))")]
    public void Create_Should_EmitOneClause_When_ASingleTraceFlagIsSet(
        bool hasError,
        int? minDurationMs,
        string? search,
        string? service,
        string expected)
    {
        // act
        var filter = CompiledTelemetryFilter.Create(null, hasError, minDurationMs, search, service);

        // assert
        Assert.Equal(expected, FilterInputFormatter.Describe(filter.Input));
    }

    [Theory]
    [InlineData("abc", null, "attribute(trace.id,eq(string:abc))")]
    [InlineData(null, "timeout", "attribute(log.message,matches(*timeout*))")]
    public void Create_Should_EmitOneClause_When_ASingleLogFlagIsSet(string? traceId, string? search, string expected)
    {
        // act
        var filter = CompiledTelemetryFilter.Create(filterText: null, severity: null, traceId, search, service: null);

        // assert
        Assert.Equal(expected, FilterInputFormatter.Describe(filter.Input));
    }

    [Fact]
    public void Create_Should_AndEveryTraceFlagAroundTheParsedTree_When_FlagsAreProvided()
    {
        // act
        var filter = CompiledTelemetryFilter.Create(
            "http.status_code:>=500",
            hasError: true,
            minDurationMs: 1000,
            search: "timeout",
            service: "checkout");

        // assert
        FilterInputFormatter
            .Serialize(filter.Input)
            .MatchInlineSnapshot(
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
                          "matches": "*timeout*"
                        },
                        "key": "span.name"
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
    public void Create_Should_AndEveryLogFlagAroundTheParsedTree_When_FlagsAreProvided()
    {
        // act
        var filter = CompiledTelemetryFilter.Create(
            "http.status_code:>=500",
            severity: "warn",
            traceId: "abc",
            search: "timeout",
            service: "checkout");

        // assert
        FilterInputFormatter
            .Serialize(filter.Input)
            .MatchInlineSnapshot(
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
    public void Create_Should_CompileTheFilter_When_FilterTextIsProvided()
    {
        // act
        var filter = CompiledTelemetryFilter.Create(TelemetryFilterSignal.Traces, "http.statuscode:>=500");

        // assert
        FilterInputFormatter.Describe(filter.Input).MatchInlineSnapshot("attribute(http.statuscode,gte(int:500))");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_Should_HaveNoInput_When_TheFilterTextIsNullOrWhitespace(string? filterText)
    {
        // act
        var filter = CompiledTelemetryFilter.Create(TelemetryFilterSignal.Traces, filterText);

        // assert
        Assert.Null(filter.Input);
    }

    [Fact]
    public void Create_Should_Throw_When_TheSeverityIsUnsupported()
    {
        // act
        var error =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CompiledTelemetryFilter.Create(
                    filterText: null,
                    severity: "verbose",
                    traceId: null,
                    search: null,
                    service: null)
            );

        // assert
        Assert.Equal("severity", error.ParamName);
        Assert.Equal("verbose", error.ActualValue);
    }

    [Theory]
    [InlineData(
        "trace",
        "attribute(severity,in(string:trace,string:debug,string:info,string:warn,string:error,string:fatal))")]
    [InlineData(
        "TRACE",
        "attribute(severity,in(string:trace,string:debug,string:info,string:warn,string:error,string:fatal))")]
    [InlineData("debug", "attribute(severity,in(string:debug,string:info,string:warn,string:error,string:fatal))")]
    [InlineData("info", "attribute(severity,in(string:info,string:warn,string:error,string:fatal))")]
    [InlineData("warn", "attribute(severity,in(string:warn,string:error,string:fatal))")]
    [InlineData("error", "attribute(severity,in(string:error,string:fatal))")]
    [InlineData("fatal", "attribute(severity,in(string:fatal))")]
    public void Create_Should_IncludeTheRequestedLevelAndEveryHigherLevel_When_ASeverityIsGiven(
        string severity,
        string expected)
    {
        // act
        var filter = CompiledTelemetryFilter.Create(
            filterText: null,
            severity,
            traceId: null,
            search: null,
            service: null);

        // assert
        Assert.Equal(expected, FilterInputFormatter.Describe(filter.Input));
    }
}
