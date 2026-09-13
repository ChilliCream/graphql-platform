using System.Reflection;
using HotChocolate.Execution;

namespace HotChocolate.Types;

public class SourceGeneratorBatchResolverTests
{
    [Fact]
    public async Task BatchResolver_Should_Bind_Argument_Per_Context_When_SourceGenerated()
    {
        // arrange
        // each aliased sibling must receive its own argument value and each parent its own
        // positional result.
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
                    List<string> prefix)
                    => brands.Zip(prefix, (b, p) => $"{p}:{b.Name}").ToList();
            }
            """,
            "SourceGeneratorBatchArgumentRepro");

        // act
        var result = await TestHelper.ExecuteSourceGeneratedAsync(
            assembly,
            """
            {
                brands {
                    name
                    x: label(prefix: "x")
                    y: label(prefix: "y")
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
                    "name": "Acme",
                    "x": "x:Acme",
                    "y": "y:Acme"
                  },
                  {
                    "name": "Globex",
                    "x": "x:Globex",
                    "y": "y:Globex"
                  }
                ]
              }
            }
            """);
    }

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
        Assert.Collection(
            exception.Errors,
            error => Assert.Contains(
                "The batch resolver method 'Repro.BrandNode.GetLabel' must return a list type "
                + "(e.g. List<T>, IReadOnlyList<T>, ImmutableArray<T> or T[]). Batch resolvers "
                + "return one result per parent object, so the return type must be a collection.",
                error.Message,
                StringComparison.Ordinal));
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
        Assert.Collection(
            exception.Errors,
            error => Assert.Contains(
                "The batch resolver method 'Repro.BrandNode.GetLabel' must return a list type "
                + "(e.g. List<T>, IReadOnlyList<T>, ImmutableArray<T> or T[]). Batch resolvers "
                + "return one result per parent object, so the return type must be a collection.",
                error.Message,
                StringComparison.Ordinal));
    }

    [Fact]
    public void BatchResolver_Should_NameNestedDeclaringType_LikeReflection_When_ReturnTypeIsNotIList()
    {
        // arrange
        // ThrowHelper (reflection path) names a nested declaring type via Type.FullName ("Outer+
        // Inner"); the generated schema error must match it byte for byte instead of using the
        // display-string form ("Outer.Inner").
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

        const string expectedMessage =
            "The batch resolver method 'TestNamespace.Container+BrandNode.GetLabel' must return a "
            + "list type (e.g. List<T>, IReadOnlyList<T>, ImmutableArray<T> or T[]). Batch "
            + "resolvers return one result per parent object, so the return type must be a "
            + "collection.";

        string? generatedSource = null;

        // act
        TestHelper.GetGeneratedSourceSnapshot(
            source,
            compilation => generatedSource = compilation.SyntaxTrees
                .Select(tree => tree.ToString())
                .FirstOrDefault(text => text.Contains("GetLabel()", StringComparison.Ordinal)));

        // assert
        Assert.Contains(expectedMessage, generatedSource, StringComparison.Ordinal);
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
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "singularProducts": {
                      "edges": [
                        {
                          "node": {
                            "id": 0
                          }
                        },
                        {
                          "node": {
                            "id": 1
                          }
                        },
                        {
                          "node": {
                            "id": 2
                          }
                        },
                        {
                          "node": {
                            "id": 3
                          }
                        },
                        {
                          "node": {
                            "id": 4
                          }
                        },
                        {
                          "node": {
                            "id": 5
                          }
                        },
                        {
                          "node": {
                            "id": 6
                          }
                        },
                        {
                          "node": {
                            "id": 7
                          }
                        },
                        {
                          "node": {
                            "id": 8
                          }
                        },
                        {
                          "node": {
                            "id": 9
                          }
                        },
                        {
                          "node": {
                            "id": 10
                          }
                        },
                        {
                          "node": {
                            "id": 11
                          }
                        },
                        {
                          "node": {
                            "id": 12
                          }
                        },
                        {
                          "node": {
                            "id": 13
                          }
                        },
                        {
                          "node": {
                            "id": 14
                          }
                        },
                        {
                          "node": {
                            "id": 15
                          }
                        },
                        {
                          "node": {
                            "id": 16
                          }
                        },
                        {
                          "node": {
                            "id": 17
                          }
                        },
                        {
                          "node": {
                            "id": 18
                          }
                        },
                        {
                          "node": {
                            "id": 19
                          }
                        }
                      ]
                    },
                    "batchProducts": {
                      "edges": [
                        {
                          "node": {
                            "id": 0
                          }
                        },
                        {
                          "node": {
                            "id": 1
                          }
                        },
                        {
                          "node": {
                            "id": 2
                          }
                        },
                        {
                          "node": {
                            "id": 3
                          }
                        },
                        {
                          "node": {
                            "id": 4
                          }
                        },
                        {
                          "node": {
                            "id": 5
                          }
                        },
                        {
                          "node": {
                            "id": 6
                          }
                        },
                        {
                          "node": {
                            "id": 7
                          }
                        },
                        {
                          "node": {
                            "id": 8
                          }
                        },
                        {
                          "node": {
                            "id": 9
                          }
                        },
                        {
                          "node": {
                            "id": 10
                          }
                        },
                        {
                          "node": {
                            "id": 11
                          }
                        },
                        {
                          "node": {
                            "id": 12
                          }
                        },
                        {
                          "node": {
                            "id": 13
                          }
                        },
                        {
                          "node": {
                            "id": 14
                          }
                        },
                        {
                          "node": {
                            "id": 15
                          }
                        },
                        {
                          "node": {
                            "id": 16
                          }
                        },
                        {
                          "node": {
                            "id": 17
                          }
                        },
                        {
                          "node": {
                            "id": 18
                          }
                        },
                        {
                          "node": {
                            "id": 19
                          }
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
    }
}
