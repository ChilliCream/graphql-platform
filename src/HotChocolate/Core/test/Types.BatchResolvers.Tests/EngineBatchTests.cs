using HotChocolate.Execution;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class EngineBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Resolve_Nested_Field_When_ParentIsList(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name greeting } }", TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        Assert.Equal(new object?[] { 1, 2, 3 }, Probe.Invocations[0].Keys);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Coalesce_Aliases_When_RootFieldHasLiteralArguments(DeclarationStyle style)
    {
        // arrange, three variable sets share one selection occurrence per alias, coalescing separately (hc-0-6cq.15)
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query($sw: Boolean!) {
                    a: productById(id: 1) @include(if: $sw) { name }
                    b: productById(id: 2) @include(if: $sw) { name }
                }
                """)
            .SetVariableValues(
                new List<IReadOnlyDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["sw"] = true },
                    new Dictionary<string, object?> { ["sw"] = true },
                    new Dictionary<string, object?> { ["sw"] = true }
                })
            .Build();

        // act
        await using var result = await ExecuteAsync(executor, request, TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResultBatch>(result);
        Assert.Collection(
            Probe.Invocations.OrderBy(i => i.Keys[0]),
            invocation => Assert.Equal(new object?[] { 1, 1, 1 }, invocation.Keys),
            invocation => Assert.Equal(new object?[] { 2, 2, 2 }, invocation.Keys));
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Coalesce_Aliases_When_RootFieldHasVariableArguments(DeclarationStyle style)
    {
        // arrange, each alias's argument varies per set but still shares one occurrence (hc-0-6cq.15)
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query($x: Int! $y: Int!) {
                    a: productById(id: $x) { name }
                    b: productById(id: $y) { name }
                }
                """)
            .SetVariableValues(
                new List<IReadOnlyDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["x"] = 1, ["y"] = 2 },
                    new Dictionary<string, object?> { ["x"] = 2, ["y"] = 1 },
                    new Dictionary<string, object?> { ["x"] = 1, ["y"] = 1 }
                })
            .Build();

        // act
        await using var result = await ExecuteAsync(executor, request, TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResultBatch>(result);
        Assert.Equal(2, Probe.Invocations.Count);
        Assert.All(Probe.Invocations, invocation => Assert.Equal(3, invocation.Keys.Count));
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Coalesce_Remaining_Siblings_When_OneAliasIsSkipped(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query($skip: Boolean!) {
                    a: productById(id: 1) @skip(if: $skip) { name }
                    b: productById(id: 2) { name }
                }
                """)
            .SetVariableValues(new Dictionary<string, object?> { { "skip", true } })
            .Build();

        // act
        await using var result = await ExecuteAsync(executor, request, TestContext.Current.CancellationToken);

        // assert
        var invocation = Assert.Single(Probe.Invocations);
        Assert.Equal([2], invocation.Keys);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "b": {
                  "name": "Product 2"
                }
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Not_Invoke_When_AllSiblingsAreSkipped(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                a: productById(id: 1) @skip(if: true) { name }
                b: productById(id: 2) @skip(if: true) { name }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(Probe.Invocations);
        result.MatchInlineSnapshot(
            """
            {
              "data": {}
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Stay_Unconditional_When_MergedFromConditionalAndUnconditional(
        DeclarationStyle style)
    {
        // arrange, the field is selected once unconditionally and once behind @include(if: $flag)
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query($flag: Boolean!) {
                    productById(id: 1) { name }
                    ... @include(if: $flag) {
                        productById(id: 1) { name }
                    }
                }
                """)
            .SetVariableValues(new Dictionary<string, object?> { { "flag", false } })
            .Build();

        // act
        await using var result = await ExecuteAsync(executor, request, TestContext.Current.CancellationToken);

        // assert
        var invocation = Assert.Single(Probe.Invocations);
        Assert.Equal([1], invocation.Keys);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "productById": {
                  "name": "Product 1"
                }
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Bind_Argument_When_ParameterIsList(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            { users { name greetingWithArgument(prefix: "Hi") } }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greetingWithArgument": "Hi, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greetingWithArgument": "Hi, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greetingWithArgument": "Hi, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Inject_Service_When_ParameterIsService(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name greetingWithService } }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greetingWithService": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greetingWithService": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greetingWithService": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Inject_GlobalState_When_ParameterIsGlobalState(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument("{ users { name greetingWithGlobalState } }")
            .SetGlobalState("prefix", "Hey")
            .Build();

        // act
        await using var result = await ExecuteAsync(executor, request, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greetingWithGlobalState": "Hey, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greetingWithGlobalState": "Hey, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greetingWithGlobalState": "Hey, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Inject_ScopedState_When_ParentResolverSetsScopedState(
        DeclarationStyle style)
    {
        // arrange
        // the "users" resolver sets scoped context data that the batch resolver below it reads.
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name greetingWithScopedState } }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greetingWithScopedState": "Hello, Alice!!!"
                  },
                  {
                    "name": "Bob",
                    "greetingWithScopedState": "Hello, Bob!!!"
                  },
                  {
                    "name": "Charlie",
                    "greetingWithScopedState": "Hello, Charlie!!!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Inject_CancellationToken_When_ParameterIsCancellationToken(
        DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name greetingWithCancellationToken } }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greetingWithCancellationToken": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greetingWithCancellationToken": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greetingWithCancellationToken": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Resolve_When_ReturnTypeIsTaskOfList(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name asyncGreeting } }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "asyncGreeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "asyncGreeting": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "asyncGreeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Resolve_When_ReturnTypeIsValueTaskOfList(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name asyncValueGreeting } }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "asyncValueGreeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "asyncValueGreeting": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "asyncValueGreeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Fail_All_Contexts_When_ResultCountMismatches(DeclarationStyle style)
    {
        // arrange, deliberately returns fewer results than contexts so every context must fail
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name mismatchGreeting } }", TestContext.Current.CancellationToken);

        // assert
        // every style raises the identical count-mismatch cause and shares the same
        // non-null-propagation shape: the errored field nulls its parent up to the root.
        var operation = Assert.IsType<OperationResult>(result);
        Assert.Equal(3, operation.Errors?.Count);
        Assert.All(
            operation.Errors!,
            error => Assert.Contains(
                "A batch resolver must return exactly one result per context. Expected 3 results but got 2.",
                error.Exception?.Message));
        result.MatchMarkdownSnapshot(style);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Resolve_When_ParentIsIReadOnlyList(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name readOnlyGreeting } }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "readOnlyGreeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "readOnlyGreeting": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "readOnlyGreeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Resolve_When_ParentIsArray(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name arrayGreeting } }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "arrayGreeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "arrayGreeting": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "arrayGreeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Resolve_When_ParentIsImmutableArray(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ users { name immutableGreeting } }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "immutableGreeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "immutableGreeting": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "immutableGreeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Coalesce_When_DeclaredInsideMutationPayload(DeclarationStyle style)
    {
        // arrange
        // a batch field inside a single mutation payload selection set coalesces below the
        // serial root resolver into one batch invocation and completes.
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "mutation { doIt { items { id stock } } }", TestContext.Current.CancellationToken);

        // assert
        var invocation = Assert.Single(Probe.Invocations);
        Assert.Equal(new object?[] { 1, 2, 3 }, invocation.Keys);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "doIt": {
                  "items": [
                    {
                      "id": 1,
                      "stock": 100
                    },
                    {
                      "id": 2,
                      "stock": 200
                    },
                    {
                      "id": 3,
                      "stock": 300
                    }
                  ]
                }
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Complete_When_ParentResolverIsAsync(DeclarationStyle style)
    {
        // arrange, representative of the #9892 regression: an async parent feeds a nested batch field
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor, "{ asyncUsers { name greeting } }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "asyncUsers": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Isolate_Batch_When_AliasedMutationRootFieldsShareSelection(
        DeclarationStyle style)
        => await AssertSerialBatchesAsync(style, aliasSameField: true);

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Isolate_Batch_When_SequentialMutationRootFieldsDiffer(
        DeclarationStyle style)
        => await AssertSerialBatchesAsync(style, aliasSameField: false);

    private async Task AssertSerialBatchesAsync(DeclarationStyle style, bool aliasSameField)
    {
        // arrange, each serial mutation step's batch must complete before the next step starts (hc-0-6cq.18)
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var secondField = aliasSameField ? "createParent" : "cloneParent";

        // act
        await using var result = await ExecuteAsync(
            executor,
            $$"""
            mutation {
                a: createParent(id: 1) { id children { id computed } }
                b: {{secondField}}(id: 2) { id children { id computed } }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result);
        Assert.Equal([2, 2], SerialProbe.BatchSizes);
        Assert.Equal(
            ["mutation-1-start", "batch-1-complete", "mutation-2-start", "batch-2-complete"],
            SerialProbe.Events);
    }
}
