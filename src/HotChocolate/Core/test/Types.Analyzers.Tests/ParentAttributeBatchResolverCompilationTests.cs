using Microsoft.CodeAnalysis;

namespace HotChocolate.Types;

// A [BatchResolver] method's [Parent] parameter supports T[], ImmutableArray<T> and
// IReadOnlyList<T>, in addition to List<T>; the generated code must compile for each shape.
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
