using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class SourceGeneratorBatchResolverTests
{
    [Fact]
    public async Task BatchResolver_Should_RaiseSchemaError_When_ReturnTypeIsNotIList()
    {
        // arrange
        // a [BatchResolver] returning a non-IList shape (here a lazy IEnumerable<T>) must raise a
        // build-time schema error naming the member.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Linq;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new()
                    {
                        new Brand { Id = 1, Name = "Acme" },
                        new Brand { Id = 2, Name = "Globex" }
                    };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static IEnumerable<string> GetLabel([Parent] List<Brand> brands)
                    => brands.Select(b => $"label-{b.Name}");
            }
            """,
            "SourceGeneratorBatchNonListRepro");

        // act
        async Task Fail() => await TestHelper.ExecuteSourceGeneratedAsync(assembly, "{ brands { name label } }");

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(Fail);
        var expected = BatchResolverErrors
            .ReturnTypeMustBeList(assembly.GetType("Repro.BrandNode")!, "GetLabel")
            .Errors[0]
            .Message;
        Assert.Collection(
            exception.Errors,
            error => Assert.Contains(expected, error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task BatchResolver_Should_RaiseSchemaError_When_ReturnTypeIsHashSet()
    {
        // arrange
        // a [BatchResolver] returning a non-list shape (here HashSet<T>) must raise a build-time
        // schema error naming the member.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Linq;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new()
                    {
                        new Brand { Id = 1, Name = "Acme" },
                        new Brand { Id = 2, Name = "Globex" }
                    };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static HashSet<string> GetLabel([Parent] List<Brand> brands)
                    => new(brands.Select(b => $"label-{b.Name}"));
            }
            """,
            "SourceGeneratorBatchHashSetRepro");

        // act
        async Task Fail() => await TestHelper.ExecuteSourceGeneratedAsync(assembly, "{ brands { name label } }");

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(Fail);
        var expected = BatchResolverErrors
            .ReturnTypeMustBeList(assembly.GetType("Repro.BrandNode")!, "GetLabel")
            .Errors[0]
            .Message;
        Assert.Collection(
            exception.Errors,
            error => Assert.Contains(expected, error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task BatchResolver_Should_RaiseSchemaError_When_ParentIsHashSet()
    {
        // arrange
        // a [BatchResolver] whose [Parent] parameter is a non-list shape (here HashSet<T>) must
        // raise a build-time schema error naming the member and parameter, never a compile error
        // in the generated code.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Linq;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new()
                    {
                        new Brand { Id = 1, Name = "Acme" },
                        new Brand { Id = 2, Name = "Globex" }
                    };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static List<string> GetLabel([Parent] HashSet<Brand> brands)
                    => brands.Select(b => $"label-{b.Name}").ToList();
            }
            """,
            "SourceGeneratorBatchParentHashSetRepro");

        // act
        async Task Fail() => await TestHelper.ExecuteSourceGeneratedAsync(assembly, "{ brands { name label } }");

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(Fail);
        var expected = BatchResolverErrors
            .ArgumentMustBeList(assembly.GetType("Repro.BrandNode")!, "GetLabel", "brands")
            .Errors[0]
            .Message;
        Assert.Collection(
            exception.Errors,
            error => Assert.Contains(expected, error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task BatchResolver_Should_RaiseSchemaError_When_ArgumentIsHashSet()
    {
        // arrange
        // a [BatchResolver] whose argument parameter is a non-list shape (here HashSet<T>) must
        // raise a build-time schema error naming the member and parameter, never a compile error
        // in the generated code.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Linq;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new()
                    {
                        new Brand { Id = 1, Name = "Acme" },
                        new Brand { Id = 2, Name = "Globex" }
                    };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static List<string> GetLabel(
                    [Parent] List<Brand> brands,
                    HashSet<string> prefix)
                    => brands.Select(b => $"label-{b.Name}").ToList();
            }
            """,
            "SourceGeneratorBatchArgumentHashSetRepro");

        // act
        async Task Fail()
            => await TestHelper.ExecuteSourceGeneratedAsync(assembly, "{ brands { name label(prefix: \"x\") } }");

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(Fail);
        var expected = BatchResolverErrors
            .ArgumentMustBeList(assembly.GetType("Repro.BrandNode")!, "GetLabel", "prefix")
            .Errors[0]
            .Message;
        Assert.Collection(
            exception.Errors,
            error => Assert.Contains(expected, error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task BatchResolver_Should_DistributeResults_When_ReturnTypeIsImmutableArray()
    {
        // arrange
        // ImmutableArray<T> is a non-nullable value type, so the batch result distribution must
        // not pattern-match it against null (CS0037 in generated code); the generated accessor
        // must still compile and distribute results correctly.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Immutable;
            using System.Linq;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            [QueryType]
            public static partial class Query
            {
                public static System.Collections.Generic.List<Brand> GetBrands()
                    => new()
                    {
                        new Brand { Id = 1, Name = "Acme" },
                        new Brand { Id = 2, Name = "Globex" }
                    };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static ImmutableArray<string> GetLabel([Parent] ImmutableArray<Brand> brands)
                    => brands.Select(b => $"label-{b.Name}").ToImmutableArray();
            }
            """,
            "SourceGeneratorBatchImmutableArrayReturnRepro");

        // act
        var result = await TestHelper.ExecuteSourceGeneratedAsync(assembly, "{ brands { name label } }");

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
        operationResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Acme",
                    "label": "label-Acme"
                  },
                  {
                    "name": "Globex",
                    "label": "label-Globex"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public void BatchResolver_Should_NameNestedDeclaringType_LikeReflection_When_ReturnTypeIsNotIList()
    {
        // arrange
        // the generator must pass the fully nested declaring type and method name to the shared
        // helper (HotChocolate.Resolvers.BatchResolverErrors), the same helper ThrowHelper
        // delegates to on the reflection path, so both render "Outer+Inner" by construction.
        const string source =
            """
            using System.Collections.Generic;
            using System.Linq;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            public static partial class Container
            {
                [ObjectType<Brand>]
                public static partial class BrandNode
                {
                    [BatchResolver]
                    public static IEnumerable<string> GetLabel([Parent] List<Brand> brands)
                        => brands.Select(b => $"label-{b.Name}");
                }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }
            """;

        string? generatedSource = null;

        // act
        TestHelper.GetGeneratedSourceSnapshot(
            source,
            compilation => generatedSource = compilation.SyntaxTrees
                .Select(tree => tree.ToString())
                .FirstOrDefault(text => text.Contains("GetLabel()", StringComparison.Ordinal)));

        // assert
        Assert.Contains(
            "global::HotChocolate.Resolvers.BatchResolverErrors.ReturnTypeMustBeList("
            + "typeof(global::TestNamespace.Container.BrandNode), \"GetLabel\")",
            generatedSource,
            StringComparison.Ordinal);

        // the shared helper renders any nested declaring type the same way, proving the argument
        // above yields the reflection-style "+" nesting rather than the display-string "." form.
        var nestedTypeMessage = BatchResolverErrors
            .ReturnTypeMustBeList(typeof(NestedDeclaringTypeFixture.BrandNode), "GetLabel")
            .Errors[0]
            .Message;
        Assert.Contains(
            $"'{typeof(NestedDeclaringTypeFixture.BrandNode).FullName}.GetLabel'",
            nestedTypeMessage,
            StringComparison.Ordinal);
        Assert.Contains("+", typeof(NestedDeclaringTypeFixture.BrandNode).FullName, StringComparison.Ordinal);
    }

    private static class NestedDeclaringTypeFixture
    {
        public static class BrandNode;
    }

    [Fact]
    public async Task BatchResolver_Should_ThrowResultCountMismatch_When_ListLengthDoesNotMatchContexts()
    {
        // arrange
        // a [BatchResolver] must return exactly one result per context; returning fewer entries
        // than parents must throw instead of silently null-filling.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Linq;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new()
                    {
                        new Brand { Id = 1, Name = "Acme" },
                        new Brand { Id = 2, Name = "Globex" }
                    };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static List<string> GetLabel([Parent] List<Brand> brands)
                    => brands.Take(1).Select(b => $"label-{b.Name}").ToList();
            }
            """,
            "SourceGeneratorBatchResultCountMismatchRepro");

        // act
        var result = await TestHelper.ExecuteSourceGeneratedAsync(assembly, "{ brands { name label } }");

        // assert
        // the mismatch is reported as a field error on the response.
        var operationResult = result.ExpectOperationResult();
        Assert.NotNull(operationResult.Errors);
        Assert.Contains(
            operationResult.Errors!,
            error => error.Exception is InvalidOperationException
                && error.Exception.Message.Equals(
                    "A batch resolver must return exactly one result per context. Expected 2 results but got 1.",
                    StringComparison.Ordinal));
    }

    [Fact]
    public async Task BatchResolver_Should_ThrowResultCountMismatch_When_ImmutableArrayResultIsDefault()
    {
        // arrange
        // a default(ImmutableArray<T>) result has no backing array; it must be reported as the
        // same count mismatch as any other wrong-length result, never a NullReferenceException.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new()
                    {
                        new Brand { Id = 1, Name = "Acme" },
                        new Brand { Id = 2, Name = "Globex" }
                    };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static ImmutableArray<string> GetLabel([Parent] List<Brand> brands)
                    => default;
            }
            """,
            "SourceGeneratorBatchImmutableArrayDefaultRepro");

        // act
        var result = await TestHelper.ExecuteSourceGeneratedAsync(assembly, "{ brands { name label } }");

        // assert
        // the mismatch is reported as a field error on the response, not a NullReferenceException.
        var operationResult = result.ExpectOperationResult();
        Assert.NotNull(operationResult.Errors);
        Assert.Contains(
            operationResult.Errors!,
            error => error.Exception is InvalidOperationException
                && error.Exception.Message.Equals(
                    "A batch resolver must return exactly one result per context. Expected 2 results but got 0.",
                    StringComparison.Ordinal));
    }

    [Fact]
    public async Task BatchResolver_Should_AssignNullToEveryContext_When_ResultIsNull()
    {
        // arrange
        // a null batch result must assign null to every context's Result, matching the
        // reflection path (BatchResolverCompiler.DistributeList), instead of leaving results
        // undistributed.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new()
                    {
                        new Brand { Id = 1, Name = "Acme" },
                        new Brand { Id = 2, Name = "Globex" }
                    };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static List<string?>? GetLabel([Parent] List<Brand> brands)
                    => null;
            }
            """,
            "SourceGeneratorBatchNullResultRepro");

        // act
        var result = await TestHelper.ExecuteSourceGeneratedAsync(assembly, "{ brands { name label } }");

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
        operationResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Acme",
                    "label": null
                  },
                  {
                    "name": "Globex",
                    "label": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Distribute_Results_When_ReturnTypeIsTaskOfList()
    {
        // arrange
        // schema-build element-type inference must unwrap Task<>, not just ValueTask<>.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new()
                    {
                        new Brand { Id = 1, Name = "Acme" },
                        new Brand { Id = 2, Name = "Globex" }
                    };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static Task<List<string>> GetLabel([Parent] List<Brand> brands)
                    => Task.FromResult(brands.Select(b => $"label-{b.Name}").ToList());
            }
            """,
            "SourceGeneratorBatchTaskAsyncInferenceRepro");

        // act
        var result = await TestHelper.ExecuteSourceGeneratedAsync(
            assembly,
            "{ brands { name label } }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Acme",
                    "label": "label-Acme"
                  },
                  {
                    "name": "Globex",
                    "label": "label-Globex"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Bind_IsSelected_When_SourceGenerated()
    {
        // arrange
        // [IsSelected] checks a child field within the current field's own composite selection.
        var assembly = CompileIsSelectedRepro("SourceGeneratorBatchIsSelectedRepro");

        // act
        var result = await TestHelper.ExecuteSourceGeneratedAsync(
            assembly,
            """
            {
                brands {
                    manager {
                        name
                        email
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "manager": {
                      "name": "Acme",
                      "email": "Acme@example.com"
                    }
                  },
                  {
                    "manager": {
                      "name": "Globex",
                      "email": "Globex@example.com"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Bind_IsSelected_When_ConditionsSpanOverflowWords()
    {
        // arrange
        // 64 filler conditions occupy Word0 (indexes 0-63), so the condition guarding "email"
        // lands in Overflow; the batch [IsSelected] binding must thread the full ConditionFlags,
        // not just Word0, to see it.
        const int fillerCount = 64;
        var assembly = CompileIsSelectedRepro("SourceGeneratorBatchIsSelectedOverflowRepro");

        var fillerVariables = string.Join(
            ", ",
            Enumerable.Range(0, fillerCount).Select(i => $"$u{i}: Boolean = false"));
        var fillerFields = string.Concat(
            Enumerable.Range(0, fillerCount)
                .Select(i => $"                    f{i}: id @include(if: $u{i})\n"));

        // act
        var result = await TestHelper.ExecuteSourceGeneratedAsync(
            assembly,
            $$"""
            query($real: Boolean = true, {{fillerVariables}}) {
                brands {
            {{fillerFields}}                manager {
                        name
                        email @include(if: $real)
                    }
                }
            }
            """);

        // assert
        // every filler field is excluded, so the shape matches the non-overflow proof above.
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "manager": {
                      "name": "Acme",
                      "email": "Acme@example.com"
                    }
                  },
                  {
                    "manager": {
                      "name": "Globex",
                      "email": "Globex@example.com"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Bind_CustomParameter_From_Union_SelectionContext_When_SourceGenerated()
    {
        // arrange
        // a custom IParameterBindingFactory (ArgumentKind.Custom) must read IsSelected flags
        // from the union batch selection context, not contexts[0], so it sees every variable
        // set's own selection rather than only the first context's.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Linq;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            public sealed class Manager
            {
                public string Name { get; set; } = default!;
                public string Email { get; set; } = default!;
                public bool BothSelected { get; set; }
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new() { new Brand { Id = 1, Name = "Acme" } };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static List<Manager> GetManager(
                    [Parent] List<Brand> brands,
                    bool bothSelected)
                    => brands
                        .Select(b => new Manager
                        {
                            Name = b.Name,
                            Email = $"{b.Name}@example.com",
                            BothSelected = bothSelected
                        })
                        .ToList();
            }
            """,
            "SourceGeneratorBatchCustomParameterUnionRepro");

        var builder = new ServiceCollection().AddGraphQLServer(disableDefaultSecurity: true);

        // matched by parameter type (bool), so it binds the unattributed "bothSelected"
        // parameter above; its binding observes only what the union selection context reports.
        builder.Services.AddSingleton<IParameterExpressionBuilder>(
            new CustomParameterExpressionBuilder<bool>(
                ctx => ctx.IsSelected("name") && ctx.IsSelected("email")));

        var addModuleMethod = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: true, IsSealed: true }
                && t.Namespace == "Microsoft.Extensions.DependencyInjection")
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Single(m =>
            {
                var p = m.GetParameters();
                return m.Name.StartsWith("Add", StringComparison.Ordinal)
                    && m.ReturnType == typeof(IRequestExecutorBuilder)
                    && p.Length == 1
                    && p[0].ParameterType == typeof(IRequestExecutorBuilder);
            });

        addModuleMethod.Invoke(null, [builder]);

        var executor = await builder.BuildRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        // one brand, two variable sets sharing the same "manager" selection occurrence: set 0
        // includes only "name", set 1 includes only "email". Both land in a single batch call.
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($wantsName: Boolean!, $wantsEmail: Boolean!) {
                        brands {
                            manager {
                                name @include(if: $wantsName)
                                email @include(if: $wantsEmail)
                                bothSelected
                            }
                        }
                    }
                    """)
                .SetVariableValues(new List<IReadOnlyDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["wantsName"] = true, ["wantsEmail"] = false },
                    new Dictionary<string, object?> { ["wantsName"] = false, ["wantsEmail"] = true }
                })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        // both variable sets must observe "bothSelected: true", proving the custom binding saw
        // the union of both sets' selections rather than only its own set's context.
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Equal(2, batch.Results.Count);
        new Snapshot()
            .Add(batch.Results[0], "Set 0 (wantsName)")
            .Add(batch.Results[1], "Set 1 (wantsEmail)")
            .MatchMarkdownSnapshot();
    }

    private static Assembly CompileIsSelectedRepro(string assemblyName)
        => TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Linq;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            public sealed class Manager
            {
                public string Name { get; set; } = default!;
                public string? Email { get; set; }
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new()
                    {
                        new Brand { Id = 1, Name = "Acme" },
                        new Brand { Id = 2, Name = "Globex" }
                    };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [BatchResolver]
                public static List<Manager> GetManager(
                    [Parent] List<Brand> brands,
                    [IsSelected("email")] bool wantsEmail)
                    => brands
                        .Select(b => new Manager
                        {
                            Name = b.Name,
                            Email = wantsEmail ? $"{b.Name}@example.com" : null
                        })
                        .ToList();
            }
            """,
            assemblyName);

    [Fact]
    public async Task PagingArguments_Should_ClampToMaxPageSize_When_FirstAndLastOmitted_OnBothEmissionSites()
    {
        // arrange
        // [UseConnection(DefaultPageSize = 100, MaxPageSize = 20)] must clamp the effective page
        // size to 20 (Math.Min) when first/last are omitted, for both a singular resolver and a
        // [BatchResolver], each binding PagingArguments directly (implicit PageConnection<T>
        // paging), not just the classic [UsePaging] middleware path.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using System.Linq;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; } = default!;
            }

            public sealed class Product
            {
                public int Id { get; set; }
            }

            [QueryType]
            public static partial class Query
            {
                public static List<Brand> GetBrands()
                    => new() { new Brand { Id = 1, Name = "Acme" } };
            }

            [ObjectType<Brand>]
            public static partial class BrandNode
            {
                [UseConnection(DefaultPageSize = 100, MaxPageSize = 20)]
                public static PageConnection<Product> GetSingularProducts(
                    [Parent] Brand brand,
                    PagingArguments arguments)
                    => new(CreatePage(arguments));

                [BatchResolver]
                [UseConnection(DefaultPageSize = 100, MaxPageSize = 20)]
                public static Task<List<PageConnection<Product>>> GetBatchProductsAsync(
                    [Parent] List<Brand> brands,
                    PagingArguments arguments)
                {
                    var connection = new PageConnection<Product>(CreatePage(arguments));
                    return Task.FromResult(brands.Select(_ => connection).ToList());
                }

                private static Page<Product> CreatePage(PagingArguments arguments)
                    => Page<Product>.Create(
                        Enumerable.Range(0, arguments.First ?? 0)
                            .Select(id => new Product { Id = id })
                            .ToImmutableArray(),
                        hasNextPage: false,
                        hasPreviousPage: false,
                        createCursor: p => p.Id.ToString());
            }
            """,
            "SourceGeneratorPagingArgumentsClampRepro");

        // act
        var result = await TestHelper.ExecuteSourceGeneratedAsync(
            assembly,
            """
            {
                brands {
                    singularProducts { edges { node { id } } }
                    batchProducts { edges { node { id } } }
                }
            }
            """);

        // assert
        // DefaultPageSize 100 is clamped down to MaxPageSize 20 on both emission sites, so
        // exactly 20 edges (ids 0-19) come back for each even though first/last were omitted.
        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task BatchResolver_Should_ResolveOnce_When_DeclaredOnInterfacePartial()
    {
        // arrange
        // a static [BatchResolver] method declared inside an [InterfaceType<T>] partial must
        // dispatch through a BatchFieldDelegate once per partition for every implementing object
        // type resolved through the interface, not once per parent (HC0053 parent-cast failure).
        // The batch pipeline partitions by concrete object type, so mixing Person and Robot
        // behind the same [Parent] List<IPerson> guard proves both that Person's two parents
        // still share a single call and that adding a second implementing type does not regress
        // that into one call per parent.
        var assembly = TestHelper.CompileBatchAssembly(
            """
            using System.Collections.Generic;
            using System.Linq;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            public static class InvocationCounter
            {
                public static int Count;
                public static List<string> Calls { get; } = new();
            }

            public interface IPerson
            {
                string Name { get; }
            }

            public sealed class Person(int id, string name) : IPerson
            {
                public int Id { get; } = id;
                public string Name { get; } = name;
            }

            public sealed class Robot(int id, string name) : IPerson
            {
                public int Id { get; } = id;
                public string Name { get; } = name;
            }

            [QueryType]
            public static partial class Query
            {
                public static List<IPerson> GetPeople()
                    => new() { new Person(1, "Alice"), new Robot(2, "Wall-E"), new Person(3, "Bob") };
            }

            [InterfaceType<IPerson>]
            public static partial class PersonInterface
            {
                [BatchResolver]
                public static List<string> GetGreeting([Parent] List<IPerson> people)
                {
                    InvocationCounter.Count++;
                    InvocationCounter.Calls.Add(string.Join(",", people.Select(p => p.Name)));
                    return people.ConvertAll(p => $"Hello, {p.Name}!");
                }
            }

            [ObjectType<Person>]
            public static partial class PersonNode;

            [ObjectType<Robot>]
            public static partial class RobotNode;
            """,
            "SourceGeneratorInterfaceBatchRepro");

        // act
        var result = await TestHelper.ExecuteSourceGeneratedAsync(
            assembly,
            "{ people { __typename name greeting } }");

        // assert
        var invocationCount = (int)assembly.GetType("Repro.InvocationCounter")!
            .GetField("Count")!
            .GetValue(null)!;
        var calls = (List<string>)assembly.GetType("Repro.InvocationCounter")!
            .GetProperty("Calls")!
            .GetValue(null)!;
        Assert.Equal(2, invocationCount);
        Assert.Contains("Alice,Bob", calls);
        Assert.Contains("Wall-E", calls);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "people": [
                  {
                    "__typename": "Person",
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "__typename": "Robot",
                    "name": "Wall-E",
                    "greeting": "Hello, Wall-E!"
                  },
                  {
                    "__typename": "Person",
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
                  }
                ]
              }
            }
            """);
    }
}
