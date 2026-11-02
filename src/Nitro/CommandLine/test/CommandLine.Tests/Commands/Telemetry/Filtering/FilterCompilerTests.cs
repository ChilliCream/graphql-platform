using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class FilterCompilerTests
{
    [Fact]
    public void Create_Should_LowerRangesAndNegatedExists_When_ThePortalSugarIsUsed()
    {
        // arrange
        const string filter = "duration:RANGE(-9, -1) -@event.exception.type:*";

        // act
        var result = CompiledTelemetryFilter.Create(TelemetryFilterSignal.Traces, filter).Input;

        // assert
        FilterInputFormatter
            .Serialize(result)
            .MatchInlineSnapshot(
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
    public void CreateFreeTextFilter_Should_ProduceDoubleStar_When_TheTextIsASingleStar()
    {
        // act
        var result = FilterCompiler.CreateFreeTextFilter("*", "log.message");

        // assert
        Assert.Equal("attribute(log.message,matches(**))", FilterInputFormatter.Describe(result));
    }

    [Theory]
    [MemberData(nameof(PortalCompilerCases))]
    public void Create_Should_ProduceTheExpectedInput_When_PortalFilterTextIsGiven(
        string filter,
        TelemetryFilterSignal signal,
        string expected)
    {
        // act
        var result = CompiledTelemetryFilter.Create(signal, filter).Input;

        // assert
        Assert.Equal(expected, FilterInputFormatter.Describe(result));
    }

    public static IEnumerable<object[]> PortalCompilerCases()
    {
        // to-graphql-input.test.ts, empty and basic expressions
        yield return ["()", TelemetryFilterSignal.Traces, "null"];
        yield return ["(((())))", TelemetryFilterSignal.Traces, "null"];
        yield return ["status:error AND ()", TelemetryFilterSignal.Traces, "attribute(status,eq(string:error))"];
        yield return ["\"\"", TelemetryFilterSignal.Traces, "null"];
        yield return ["() ()", TelemetryFilterSignal.Traces, "null"];
        yield return ["() OR ()", TelemetryFilterSignal.Traces, "null"];
        yield return ["-()", TelemetryFilterSignal.Traces, "null"];
        yield return ["() OR a:1", TelemetryFilterSignal.Traces, "attribute(a,eq(int:1))"];
        yield return
        [
            "(http.status_code:>=500 OR http.status_code:0) AND service.name:(\"a\" OR \"b\")",
            TelemetryFilterSignal.Traces,
            "and(or(attribute(http.status_code,gte(int:500)),attribute(http.status_code,eq(int:0))),attribute(service.name,in(string:a,string:b)))"
        ];
        yield return
        [
            "http.status_code:RANGE(400, 500)",
            TelemetryFilterSignal.Traces,
            "and(attribute(http.status_code,gte(int:400)),attribute(http.status_code,lte(int:500)))"
        ];
        yield return
        [
            "-duration:RANGE(1.5, 3)",
            TelemetryFilterSignal.Traces,
            "not(and(attribute(duration,gte(float:1.5)),attribute(duration,lte(int:3))))"
        ];
        yield return
        [
            "-http.url:*/health*",
            TelemetryFilterSignal.Traces,
            "not(attribute(http.url,matches(*/health*)))"
        ];
        yield return
        [
            "service.name:\"api-gateway\" timeout",
            TelemetryFilterSignal.Traces,
            "and(attribute(service.name,eq(string:api-gateway)),attribute(span.name,matches(*timeout*)))"
        ];
        yield return ["timeout", TelemetryFilterSignal.Logs, "attribute(log.message,matches(*timeout*))"];
        yield return ["duration:>1000.5", TelemetryFilterSignal.Traces, "attribute(duration,gt(float:1000.5))"];
        yield return ["error:true", TelemetryFilterSignal.Traces, "attribute(error,eq(boolean:true))"];
        yield return ["duration:2.0", TelemetryFilterSignal.Traces, "attribute(duration,eq(float:2))"];
        yield return ["x:12345678901", TelemetryFilterSignal.Traces, "attribute(x,eq(float:12345678901))"];
        yield return ["a:-0.5", TelemetryFilterSignal.Traces, "attribute(a,eq(float:-0.5))"];

        // free-text matching and explicit wildcard values
        foreach (var filter in new[] { "HTTP", "*HTTP", "HTTP*", "*HTTP*" })
        {
            yield return [filter, TelemetryFilterSignal.Logs, "attribute(log.message,matches(*HTTP*))"];
        }

        yield return ["\"a b\"", TelemetryFilterSignal.Logs, "attribute(log.message,matches(*a b*))"];
        yield return ["*test*test*", TelemetryFilterSignal.Logs, "attribute(log.message,matches(*test*test*))"];
        yield return ["a*b", TelemetryFilterSignal.Logs, "attribute(log.message,matches(*a*b*))"];
        yield return ["service.name:test*", TelemetryFilterSignal.Traces, "attribute(service.name,matches(test*))"];
        yield return
        [
            "service.name:\"test*\"",
            TelemetryFilterSignal.Traces,
            "attribute(service.name,eq(string:test*))"
        ];
        yield return ["service.name:test", TelemetryFilterSignal.Traces, "attribute(service.name,eq(string:test))"];
        yield return ["name:foo\\*", TelemetryFilterSignal.Traces, "attribute(name,eq(string:foo*))"];
        yield return ["name:foo*", TelemetryFilterSignal.Traces, "attribute(name,matches(foo*))"];
        yield return ["http.status_code:*", TelemetryFilterSignal.Traces, "attribute(http.status_code,exists(True))"];
        yield return ["-http.status_code:*", TelemetryFilterSignal.Traces, "attribute(http.status_code,exists(False))"];

        // sets, comparisons, and booleans
        yield return
        [
            "http.status_code:IN(500, 502, 503, 200)",
            TelemetryFilterSignal.Traces,
            "attribute(http.status_code,in(int:500,int:502,int:503,int:200))"
        ];
        yield return
        [
            "service.name:IN(\"api gateway\", checkout)",
            TelemetryFilterSignal.Traces,
            "attribute(service.name,in(string:api gateway,string:checkout))"
        ];
        yield return
        [
            "x:(1 OR 2.5 OR true)",
            TelemetryFilterSignal.Traces,
            "attribute(x,in(int:1,float:2.5,boolean:true))"
        ];
        yield return
        [
            "a:IN(-1, -2.5, \"-3\")",
            TelemetryFilterSignal.Traces,
            "attribute(a,in(int:-1,float:-2.5,string:-3))"
        ];
        yield return ["x:IN()", TelemetryFilterSignal.Traces, "attribute(x,in())"];
        yield return ["status:(ok)", TelemetryFilterSignal.Traces, "attribute(status,in(string:ok))"];
        yield return
        [
            "graphql.operation.name:(*ProductById OR test*)",
            TelemetryFilterSignal.Traces,
            "or(attribute(graphql.operation.name,matches(*ProductById)),attribute(graphql.operation.name,matches(test*)))"
        ];
        yield return
        [
            "status:IN(ok, err*, unset)",
            TelemetryFilterSignal.Traces,
            "or(attribute(status,in(string:ok,string:unset)),attribute(status,matches(err*)))"
        ];
        yield return ["status:(err*)", TelemetryFilterSignal.Traces, "attribute(status,matches(err*))"];
        yield return
        [
            "@span.db.name:IN(a*, b)",
            TelemetryFilterSignal.Traces,
            "or(attribute(db.name@Span,in(string:b)),attribute(db.name@Span,matches(a*)))"
        ];
        yield return
        [
            "-status:(a* OR b)",
            TelemetryFilterSignal.Traces,
            "not(or(attribute(status,in(string:b)),attribute(status,matches(a*))))"
        ];
        yield return ["status:(a\\* OR b)", TelemetryFilterSignal.Traces, "attribute(status,in(string:a*,string:b))"];
        yield return ["d:>1", TelemetryFilterSignal.Traces, "attribute(d,gt(int:1))"];
        yield return ["d:>=1", TelemetryFilterSignal.Traces, "attribute(d,gte(int:1))"];
        yield return ["d:<1", TelemetryFilterSignal.Traces, "attribute(d,lt(int:1))"];
        yield return ["d:<=1", TelemetryFilterSignal.Traces, "attribute(d,lte(int:1))"];
        yield return ["error:false", TelemetryFilterSignal.Traces, "attribute(error,eq(boolean:false))"];
        yield return
        [
            "-(a:1 OR b:2)",
            TelemetryFilterSignal.Traces,
            "not(or(attribute(a,eq(int:1)),attribute(b,eq(int:2))))"
        ];
        yield return
        [
            "service.name:\"api gateway\"",
            TelemetryFilterSignal.Traces,
            "attribute(service.name,eq(string:api gateway))"
        ];

        // scope-prefix lowering
        yield return
        [
            "@resource.service.name:\"api\"",
            TelemetryFilterSignal.Traces,
            "attribute(service.name@Resource,eq(string:api))"
        ];
        yield return ["@span.duration:>5", TelemetryFilterSignal.Traces, "attribute(duration@Span,gt(int:5))"];
        yield return
        [
            "@log.severity_number:>=17",
            TelemetryFilterSignal.Logs,
            "attribute(severity_number@Log,gte(int:17))"
        ];
        yield return
        [
            "@event.exception.type:\"TimeoutError\"",
            TelemetryFilterSignal.Traces,
            "attribute(exception.type@Event,eq(string:TimeoutError))"
        ];
        yield return ["@body.message:hello", TelemetryFilterSignal.Logs, "attribute(message@Body,eq(string:hello))"];
        yield return
        [
            "@event.exception.message:*",
            TelemetryFilterSignal.Traces,
            "attribute(exception.message@Event,exists(True))"
        ];
        yield return
        [
            "-@event.exception.escaped:*",
            TelemetryFilterSignal.Traces,
            "attribute(exception.escaped@Event,exists(False))"
        ];
        yield return ["span.name:GET", TelemetryFilterSignal.Traces, "attribute(span.name,eq(string:GET))"];
    }
}
