using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public sealed class IntrospectionTests
{
    [Theory]
    [MemberData(nameof(CostQueryData))]
    public async Task Execute_CostQuery_ReturnsExpectedResult(int index, string costQuery)
    {
        // arrange
        const string schema =
            """
            # Weights used:
            # 1.0 = (default for composite and list types)
            # 2.0 = ArgumentDefinition
            # 3.0 = FieldDefinition
            # 4.0 = Object
            # 5.0 = InputFieldDefinition
            # 6.0 = Scalar
            # 7.0 = Enum

            type Query {
                examples(limit: Int! @cost(weight: "2.0")): [Example1!]!
                    @cost(weight: "3.0") @listSize(slicingArguments: ["limit"])
            }

            type Example1 @cost(weight: "4.0") {
                example1Field1(arg1: String!, arg2: String!): Boolean!
                example1Field2(arg1Input1: Input1!): Example2!
                example1Field3: Example3!
            }

            type Example2 {
                example2Field1(arg1: String!, arg2: String!): Boolean!
                example2Field2(arg1Input2: [Input2!]!): Int!
            }

            type Example3 {
                example3Field1: Scalar1!
                example3Field2: Enum1!
            }

            input Input1 { input1Field1: String! @cost(weight: "5.0"), input1Field2: Input2! }
            input Input2 { input2Field1: String!, input2Field2: String! }

            scalar Scalar1 @cost(weight: "6.0")

            enum Enum1 @cost(weight: "7.0") { ENUM_VALUE1 }

            directive @example(dirArg1: Int!, dirArg2: Input1!) on
                | FIELD
                | FRAGMENT_DEFINITION
                | FRAGMENT_SPREAD
                | INLINE_FRAGMENT
                | QUERY
                | VARIABLE_DEFINITION
            """;

        var query =
            $$"""
            query($limit: Int! = 10 {{ExampleDirective(1)}}) {{ExampleDirective(2)}} {
                examples(limit: $limit) {{ExampleDirective(3)}} {
                    ... {{ExampleDirective(4)}} {
                        example1Field1(arg1: "", arg2: "") {{ExampleDirective(5)}}
                    }

                    # Repeated to test indexed paths (f.e. "query.examples.on~Example1[1]").
                    ... { example1Field1(arg1: "", arg2: "") }

                    example1Field2(
                        arg1Input1: {
                            input1Field1: ""
                            input1Field2: { input2Field1: "", input2Field2: "" }
                        }
                    ) {
                        ...fragment1 {{ExampleDirective(6)}}
                    }

                    example1Field3 { example3Field1, aliasField2: example3Field2 }

                    # Repeated to test indexed paths (f.e. "query.examples.example1Field3[1]")
                    example1Field3 { aliasField1: example3Field1, example3Field2 }
                }

                {{costQuery}}
            }

            fragment fragment1 on Example2 {{ExampleDirective(7)}} {
                example2Field1(arg1: "", arg2: "") {{ExampleDirective(8)}}
                example2Field2(arg1Input2: [
                    { input2Field1: "", input2Field2: "" }
                    { input2Field1: "", input2Field2: "" }
                ])
            }
            """;

        var snapshot = new Snapshot(postFix: index.ToString());

        snapshot
            .Add(schema, "Schema")
            .Add(query, "Query");

        var requestExecutor = await CreateRequestExecutorBuilder()
            .AddDocumentFromString(schema)
            .BuildRequestExecutorAsync();

        // act
        var result = await requestExecutor.ExecuteAsync(query);

        snapshot.AddResult(result.ExpectQueryResult(), "Result");

        // assert
        await snapshot.MatchMarkdownAsync();
    }

    private static string ExampleDirective(int i)
    {
        return
            $$"""@example(dirArg1: {{i}}, dirArg2: { """ +
            """input1Field1: "", input1Field2: { input2Field1: "", input2Field2: "" } })""";
    }

    public static TheoryData<int, string> CostQueryData()
    {
        return new TheoryData<int, string>
        {
            // All counts.
            {
                0,
                """
                __cost {
                    requestCosts {
                        fieldCounts { name, value }
                        typeCounts { name, value }
                        inputTypeCounts { name, value }
                        inputFieldCounts { name, value }
                        argumentCounts { name, value }
                        directiveCounts { name, value }

                        fieldCost
                        typeCost

                        fieldCostByLocation { path, cost }
                        typeCostByLocation { path, cost }
                    }
                }
                """
            },
            // Filtered field counts.
            {
                1,
                """
                __cost {
                    requestCosts {
                        example1Field1Counts: fieldCounts(regexName: "Example1\\.example1Field1")
                            { name, value }
                        fieldCountsInExample1Type: fieldCounts(regexName: "Example1\\..+")
                            { name, value }
                    }
                }
                """
            },
            // Filtered type counts.
            {
                2,
                """
                __cost {
                    requestCosts {
                        example1Counts: typeCounts(regexName: "Example1")
                            { name, value }
                        endsWithTCounts: typeCounts(regexName: ".*t")
                            { name, value }
                    }
                }
                """
            },
            // Filtered input type counts.
            {
                3,
                """
                __cost {
                    requestCosts {
                        input2Counts: inputTypeCounts(regexName: "Input2")
                            { name, value }
                    }
                }
                """
            },
            // Filtered input field counts.
            {
                4,
                """
                __cost {
                    requestCosts {
                        input1Field1Counts: inputFieldCounts(regexName: "Input1\\.input1Field1")
                            { name, value }
                        fieldCountsInInput2Type: inputFieldCounts(regexName: "Input2\\..+")
                            { name, value }
                    }
                }
                """
            },
            // Filtered argument counts.
            {
                5,
                """
                __cost {
                    requestCosts {
                        argsNamedArg1Counts: argumentCounts(regexName: ".+\\(arg1:\\)")
                            { name, value }
                        argsOnExampleDirectiveCounts: argumentCounts(regexName: "@example\\(.+")
                            { name, value }
                    }
                }
                """
            },
            // Filtered directive counts.
            {
                6,
                """
                __cost {
                    requestCosts {
                        exampleDirectiveCounts: directiveCounts(regexName: "@example")
                            { name, value }
                    }
                }
                """
            },
            // Filtered field costs by location.
            {
                7,
                """
                __cost {
                    requestCosts {
                        exampleDirectiveOnQuery:
                            fieldCostByLocation(regexPath: "query\\.@example.*")
                                { path, cost }
                        fragment1:
                            fieldCostByLocation(regexPath: ".*~fragment1.*")
                                { path, cost }
                        example1Field3:
                            fieldCostByLocation(regexPath: ".*\\.example1Field3\\[\\d+\\]")
                                { path, cost }
                    }
                }
                """
            },
            // Filtered type costs by location.
            {
                8,
                """
                __cost {
                    requestCosts {
                        examplesField:
                            typeCostByLocation(regexPath: "query\\.examples")
                                { path, cost }
                        example1Field3:
                            typeCostByLocation(regexPath: ".*\\.example1Field3\\[\\d+\\]")
                                { path, cost }
                    }
                }
                """
            }
        };
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
    {
        return new ServiceCollection()
            .AddGraphQLServer()
            .UseDefaultPipelineWithCostAnalysis()
            .AddResolver(
                "Query",
                "examples",
                _ => new List<Example1>
                {
                    new(true, new Example2(true, 1, ""), new Example3("", Enum1.EnumValue1))
                })
            .AddResolver(
                "Example1",
                "example1Field1",
                context => context.Parent<Example1>().Field1)
            .AddResolver(
                "Example1",
                "example1Field2",
                context => context.Parent<Example1>().Field2)
            .AddResolver(
                "Example1",
                "example1Field3",
                context => context.Parent<Example1>().Field3)
            .AddResolver(
                "Example2",
                "example2Field1",
                context => context.Parent<Example2>().Field1)
            .AddResolver(
                "Example2",
                "example2Field2",
                context => context.Parent<Example2>().Field2)
            .AddResolver(
                "Example2",
                "example2Field3",
                context => context.Parent<Example2>().Field3)
            .AddResolver(
                "Example3",
                "example3Field1",
                context => context.Parent<Example3>().Field1)
            .AddResolver(
                "Example3",
                "example3Field2",
                context => context.Parent<Example3>().Field2)
            .AddType<Scalar1Type>();
    }

    private sealed record Example1(bool Field1, Example2 Field2, Example3 Field3);
    private sealed record Example2(bool Field1, int Field2, string Field3);
    private sealed record Example3(string Field1, Enum1 Field2);

    private enum Enum1
    {
        EnumValue1
    }
}

public sealed class Scalar1Type() : ScalarType<string, StringValueNode>("Scalar1")
{
    public override IValueNode ParseResult(object? resultValue) => ParseValue(resultValue);

    protected override string ParseLiteral(StringValueNode valueSyntax) => valueSyntax.Value;

    protected override StringValueNode ParseValue(string runtimeValue) => new(runtimeValue);
}
