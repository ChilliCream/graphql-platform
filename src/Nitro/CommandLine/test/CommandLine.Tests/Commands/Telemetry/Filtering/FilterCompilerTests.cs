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
    [MemberData(nameof(SectionElevenCases))]
    public void Compile_Should_ProduceAnInputTree_When_ASectionElevenPortalExampleIsProvided(
        string filter,
        TelemetryFilterSignal signal,
        string expected)
    {
        // act
        var result = FilterCompiler.Compile(
            filter,
            signal == TelemetryFilterSignal.Traces ? "span.name" : "log.message",
            signal);

        // assert
        Assert.Equal(expected, Describe(result));
    }

    [Theory]
    [InlineData("duration:2.0", "attribute(duration,eq(float:2))")]
    [InlineData("x:12345678901", "attribute(x,eq(float:12345678901))")]
    [InlineData("flag:true", "attribute(flag,eq(boolean:true))")]
    [InlineData("flag:false", "attribute(flag,eq(boolean:false))")]
    [InlineData("field:foo\\ bar", "attribute(field,eq(string:foo bar))")]
    [InlineData("name:foo*", "attribute(name,matches(foo*))")]
    [InlineData("name:foo\\*", "attribute(name,eq(string:foo*))")]
    [InlineData("d:<1", "attribute(d,lt(int:1))")]
    [InlineData("d:<=1", "attribute(d,lte(int:1))")]
    [InlineData("service.name:\"test*\"", "attribute(service.name,eq(string:test*))")]
    [InlineData("status:IN(ok, err*, unset)", "or(attribute(status,in(string:ok,string:unset)),attribute(status,matches(err*)))")]
    [InlineData("status:(err*)", "attribute(status,matches(err*))")]
    [InlineData("-status:(a* OR b)", "not(or(attribute(status,in(string:b)),attribute(status,matches(a*))))")]
    [InlineData("@span.http.method:*", "attribute(http.method@Span,exists(True))")]
    [InlineData("-@resource.host.name:*", "attribute(host.name@Resource,exists(False))")]
    public void Compile_Should_PreservePortalScalarAndEscapeSemantics_When_ValuesUseSpecialForms(
        string filter,
        string expected)
    {
        // act
        var result = FilterCompiler.Compile(filter, "span.name", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(expected, Describe(result));
    }

    public static IEnumerable<object[]> SectionElevenCases()
    {
        yield return [
            "status:error status:ok",
            TelemetryFilterSignal.Traces,
            "and(attribute(status,eq(string:error)),attribute(status,eq(string:ok)))"
        ];
        yield return [
            "severity:fatal severity:error severity:warn severity:info severity:debug",
            TelemetryFilterSignal.Logs,
            "and(attribute(severity,eq(string:fatal)),attribute(severity,eq(string:error)),attribute(severity,eq(string:warn)),attribute(severity,eq(string:info)),attribute(severity,eq(string:debug)))"
        ];
        yield return [
            "service.name:\"api-gateway\" http.status_code:>=500 env:prod",
            TelemetryFilterSignal.Traces,
            "and(attribute(service.name,eq(string:api-gateway)),attribute(http.status_code,gte(int:500)),attribute(env,eq(string:prod)))"
        ];
        yield return [
            "-service.version:\"1.0.0\" duration:>1000",
            TelemetryFilterSignal.Traces,
            "and(not(attribute(service.version,eq(string:1.0.0))),attribute(duration,gt(int:1000)))"
        ];
        yield return [
            "http.status_code:IN(500, 502, 503)",
            TelemetryFilterSignal.Traces,
            "attribute(http.status_code,in(int:500,int:502,int:503))"
        ];
        yield return [
            "http.url:*/api/*",
            TelemetryFilterSignal.Traces,
            "attribute(http.url,matches(*/api/*))"
        ];
        yield return [
            "-http.status_code:404",
            TelemetryFilterSignal.Traces,
            "not(attribute(http.status_code,eq(int:404)))"
        ];
        yield return [
            "timeout",
            TelemetryFilterSignal.Traces,
            "attribute(span.name,matches(*timeout*))"
        ];
        yield return [
            "\\@span",
            TelemetryFilterSignal.Traces,
            "attribute(span.name,matches(*@span*))"
        ];
        yield return [
            "(http.status_code:>=500 OR http.status_code:0) AND service.name:(\"a\" OR \"b\")",
            TelemetryFilterSignal.Traces,
            "and(or(attribute(http.status_code,gte(int:500)),attribute(http.status_code,eq(int:0))),attribute(service.name,in(string:a,string:b)))"
        ];
        yield return [
            "graphql.operation.name:(*ProductById OR test*)",
            TelemetryFilterSignal.Traces,
            "or(attribute(graphql.operation.name,matches(*ProductById)),attribute(graphql.operation.name,matches(test*)))"
        ];
        yield return [
            "-(status:ok OR status:unset)",
            TelemetryFilterSignal.Traces,
            "not(or(attribute(status,eq(string:ok)),attribute(status,eq(string:unset))))"
        ];
        yield return [
            "duration:RANGE(-9, -1)",
            TelemetryFilterSignal.Traces,
            "and(attribute(duration,gte(int:-9)),attribute(duration,lte(int:-1)))"
        ];
        yield return [
            "@span.duration:>5",
            TelemetryFilterSignal.Traces,
            "attribute(duration@Span,gt(int:5))"
        ];
        yield return [
            "@event.exception.type:\"TimeoutError\"",
            TelemetryFilterSignal.Traces,
            "attribute(exception.type@Event,eq(string:TimeoutError))"
        ];
        yield return [
            "@log.OriginalFormat:*timeout*",
            TelemetryFilterSignal.Logs,
            "attribute(OriginalFormat@Log,matches(*timeout*))"
        ];
        yield return [
            "@body.message:hello",
            TelemetryFilterSignal.Logs,
            "attribute(message@Body,eq(string:hello))"
        ];
        yield return [
            "@resource.service.name:\"api\" @span.db.name:GET",
            TelemetryFilterSignal.Traces,
            "and(attribute(service.name@Resource,eq(string:api)),attribute(db.name@Span,eq(string:GET)))"
        ];
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
