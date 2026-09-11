using Microsoft.CodeAnalysis;

namespace HotChocolate.Types;

// REPRO: the ParentAttributeAnalyzer blesses [Parent] list shapes (T[], ImmutableArray<T>,
// IReadOnlyList<T>) and BatchResolverAttribute's own XML docs advertise IReadOnlyList<T> and T[],
// but WriteBatchResolver emits "new {ParamType}(contexts.Length)" verbatim, so only List<T> compiles.
// These tests assert the generated assembly emits cleanly (the post-fix behavior). They fail today
// because the generated .hc.g.cs contains compiler errors (CS1586 / CS1729 / CS0144).
public class ParentAttributeBatchResolverCompilationTests
{
    [Fact]
    public void BatchResolver_Should_Compile_When_ParentIsArray()
    {
        // arrange
        const string source =
            """
            using HotChocolate;
            using HotChocolate.Types;
            using System.Collections.Generic;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [BatchResolver]
                public static List<string> GetDisplayName(
                    [Parent] Product[] products)
                    => products.Select(p => p.Name).ToList();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        // act
        var diagnostics = TestHelper.GetGeneratedAssemblyEmitDiagnostics(source);

        // assert
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void BatchResolver_Should_Compile_When_ParentIsImmutableArray()
    {
        // arrange
        const string source =
            """
            using HotChocolate;
            using HotChocolate.Types;
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [BatchResolver]
                public static List<string> GetDisplayName(
                    [Parent] ImmutableArray<Product> products)
                    => products.Select(p => p.Name).ToList();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        // act
        var diagnostics = TestHelper.GetGeneratedAssemblyEmitDiagnostics(source);

        // assert
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void BatchResolver_Should_Compile_When_ParentIsIReadOnlyList()
    {
        // arrange
        const string source =
            """
            using HotChocolate;
            using HotChocolate.Types;
            using System.Collections.Generic;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [BatchResolver]
                public static List<string> GetDisplayName(
                    [Parent] IReadOnlyList<Product> products)
                    => products.Select(p => p.Name).ToList();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        // act
        var diagnostics = TestHelper.GetGeneratedAssemblyEmitDiagnostics(source);

        // assert
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }
}
