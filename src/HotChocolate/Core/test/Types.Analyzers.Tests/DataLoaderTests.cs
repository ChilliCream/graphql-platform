using HotChocolate.Types.Analyzers;
using HotChocolate.Types.Analyzers.Inspectors;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types;

public class DataLoaderTests
{
    [Fact]
    public void TryCreate_Should_CreateBatchModel_When_GenericAttributeUsesClosedKindInterface()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal static class Loaders
            {
                [DataLoader<IBatchDataLoader<int, string>>("Entity")]
                public static Task<IReadOnlyDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """;

        // act
        var info = TryCreateGenericDataLoaderInfo(source);

        // assert
        var dataLoader = Assert.IsType<GenericDataLoaderInfo>(info);
        Assert.Equal(DataLoaderKind.Batch, dataLoader.Kind);
        Assert.Equal("EntityDataLoader", dataLoader.Name);
        Assert.Equal("int", dataLoader.KeyType.ToDisplayString());
        Assert.Equal("string", dataLoader.ValueType.ToDisplayString());
    }

    [Fact]
    public void TryCreate_Should_CreateCacheModel_When_GenericAttributeUsesDerivedKindInterface()
    {
        // arrange
        const string source =
            """
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal interface IEntityLoader : ICacheDataLoader<int, string>
            {
            }

            internal static class Loaders
            {
                [DataLoader<IEntityLoader>(ServiceScope = DataLoaderServiceScope.OriginalScope, MaxBatchSize = 10)]
                public static ValueTask<string> GetEntityByIdAsync(int key)
                    => default;
            }
            """;

        // act
        var info = TryCreateGenericDataLoaderInfo(source);

        // assert
        var dataLoader = Assert.IsType<GenericDataLoaderInfo>(info);
        Assert.Equal(DataLoaderKind.Cache, dataLoader.Kind);
        Assert.Equal("EntityByIdDataLoader", dataLoader.Name);
        Assert.False(dataLoader.IsScoped);
        Assert.Equal(10, dataLoader.MaxBatchSize);
    }

    [Fact]
    public void TryCreate_Should_ReturnNull_When_GenericContractOrMethodShapeIsInvalid()
    {
        // arrange
        const string concreteTypeSource =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal sealed class EntityLoader : IBatchDataLoader<int, string>
            {
            }

            internal static class Loaders
            {
                [DataLoader<EntityLoader>]
                public static Task<IReadOnlyDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """;
        const string invalidReturnTypeSource =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal interface IEntityLoader : ICacheDataLoader<int, string>
            {
            }

            internal static class Loaders
            {
                [DataLoader<IEntityLoader>]
                public static Task<IReadOnlyDictionary<int, string>> GetEntityByIdAsync(int key)
                    => default!;
            }
            """;

        // act
        var concreteTypeInfo = TryCreateGenericDataLoaderInfo(concreteTypeSource);
        var invalidReturnTypeInfo = TryCreateGenericDataLoaderInfo(invalidReturnTypeSource);

        // assert
        Assert.Null(concreteTypeInfo);
        Assert.Null(invalidReturnTypeInfo);
    }

    [Theory]
    [MemberData(nameof(GenericDataLoaderContractCases))]
    public void TryCreate_Should_ModelExpectedContract_When_GenericDataLoaderMethodIsExamined(
        string source,
        DataLoaderKind? expectedKind)
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(source);

        // act
        var info = TryCreateGenericDataLoaderInfo(source);

        // assert
        Assert.Empty(
            compilation.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(t => t.Severity is DiagnosticSeverity.Error));

        if (expectedKind is { } kind)
        {
            var dataLoader = Assert.IsType<GenericDataLoaderInfo>(info);
            Assert.Equal(kind, dataLoader.Kind);
        }
        else
        {
            Assert.Null(info);
        }
    }

    [Fact]
    public void Generate_Should_IgnoreGenericMethod_When_OldAttributeLoaderIsValid()
    {
        // arrange
        const string source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal static class Loaders
            {
                [DataLoader<IBatchDataLoader<int, string>>]
                public static Task<IReadOnlyDictionary<int, string>> GetGenericAsync<T>(
                    IReadOnlyList<int> keys)
                    => default!;

                [DataLoader]
                public static Task<IReadOnlyDictionary<int, string>> GetOldAsync(
                    IReadOnlyList<int> keys)
                    => default!;
            }
            """;
        var compilation = TestHelper.CreateCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new GraphQLServerGenerator());

        // act
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var generatedSources = result.Results.SelectMany(t => t.GeneratedSources).ToArray();

        // assert
        Assert.Equal(2, generatedSources.Length);
        Assert.All(
            generatedSources,
            generatedSource => Assert.Contains(
                "OldDataLoader",
                generatedSource.SourceText.ToString(),
                StringComparison.Ordinal));
        Assert.Empty(result.Diagnostics);
    }

    public static IEnumerable<object?[]> GenericDataLoaderContractCases()
    {
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => default!;"),
            DataLoaderKind.Batch
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "internal interface IEntityLoader : ICacheDataLoader<int, string> { }",
                "IEntityLoader",
                "ValueTask<string> GetAsync(int key) => default;"),
            DataLoaderKind.Cache
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "ICacheDataLoader<int, string>",
                "Task<string> GetAsync(int key) => default!;"),
            DataLoaderKind.Cache
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "internal interface IEntityLoader : IBatchDataLoader<int, string> { }",
                "IEntityLoader",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => default!;"),
            DataLoaderKind.Batch
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "ValueTask<IDictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => default;"),
            DataLoaderKind.Batch
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "Task<string> GetAsync(IReadOnlyList<int> keys) => default!;"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "internal interface IInvalid : IBatchDataLoader<int, string>, ICacheDataLoader<int, string> { }",
                "IInvalid",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => default!;"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => default!;",
                """
                namespace GreenDonut
                {
                    public sealed class DataLoaderAttributeXAttribute<T> : System.Attribute { }
                }
                """,
                "DataLoaderAttributeX<IBatchDataLoader<int, string>>"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => default!;",
                """
                namespace GreenDonut
                {
                    public static class DataLoaderAttributeX
                    {
                        public sealed class NestedAttribute<T> : System.Attribute { }
                    }
                }
                """,
                "DataLoaderAttributeX.Nested<IBatchDataLoader<int, string>>"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "internal interface IReadOnlyList<T> { }",
                "IBatchDataLoader<int, string>",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => default!;"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "internal class Task<T> { }",
                "IBatchDataLoader<int, string>",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => default!;"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "Task<Dictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => default!;"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(ref IReadOnlyList<int> keys) => default!;"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(in IReadOnlyList<int> keys) => default!;"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(ref readonly IReadOnlyList<int> keys) => default!;"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(out IReadOnlyList<int> keys) { keys = default!; return default!; }"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "",
                "IBatchDataLoader<int, string>",
                "Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys, ref int marker) => default!;"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "private static Task<IReadOnlyDictionary<int, string>> s_result = default!;",
                "IBatchDataLoader<int, string>",
                "ref Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => ref s_result;"),
            null
        ];
        yield return
        [
            CreateGenericDataLoaderSource(
                "private static Task<IReadOnlyDictionary<int, string>> s_result = default!;",
                "IBatchDataLoader<int, string>",
                "ref readonly Task<IReadOnlyDictionary<int, string>> GetAsync(IReadOnlyList<int> keys) => ref s_result;"),
            null
        ];
    }

    [Fact]
    public async Task Generate_Should_LinkImplementationToAnnotatedMethod_When_SourceMethodIsOverloaded()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    [DataLoaderState("state")] int? state,
                    CancellationToken cancellationToken)
                    => default!;

                public static Task<IReadOnlyDictionary<string, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<string> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
            }
            """,
            compilation =>
            {
                var methodSyntax = compilation.SyntaxTrees
                    .SelectMany(t => t.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                    .Single(m => m.AttributeLists
                        .SelectMany(a => a.Attributes)
                        .Any(a => a.Name.ToString() == "DataLoader"));
                var sourceMethod = compilation
                    .GetSemanticModel(methodSyntax.SyntaxTree)
                    .GetDeclaredSymbol(methodSyntax);
                var generatedTree = compilation.SyntaxTrees.Single(
                    t => t.FilePath.StartsWith("GreenDonutDataLoader", StringComparison.Ordinal));
                var semanticModel = compilation.GetSemanticModel(generatedTree);
                var crefs = generatedTree
                    .GetRoot()
                    .DescendantNodes(descendIntoTrivia: true)
                    .OfType<XmlCrefAttributeSyntax>()
                    .Select(a => a.Cref)
                    .ToArray();

                var cref = Assert.Single(crefs);
                Assert.Empty(cref.GetDiagnostics());
                Assert.True(
                    SymbolEqualityComparer.Default.Equals(
                        sourceMethod,
                        semanticModel.GetSymbolInfo(cref).Symbol));
            }).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_ValueType_Result_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, long>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Nullable_ValueType_Result_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, long?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_Nullable_Object_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, Entity?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Group_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            [DataLoaderGroup("Group1")]
            internal static class TestClass
            {
                [DataLoader]
                [DataLoaderGroup("Group2")]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Group_Only_On_Class_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            [DataLoaderGroup("Group1")]
            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Group_Only_On_Method_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoaderGroup("Group1")]
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_GroupedDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<ILookup<int, Entity>> GetEntitiesByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_GroupedDataLoader_Nullable_Object_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<ILookup<int, Entity?>> GetEntitiesByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_GroupedDataLoader_ValueType_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<ILookup<int, long>> GetEntitiesByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_GroupedDataLoader_Nullable_ValueType_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<ILookup<int, long?>> GetEntitiesByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_CacheDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Entity> GetEntityByIdAsync(
                    int entityId,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_CacheDataLoader_Nullable_Object_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Entity?> GetEntityByIdAsync(
                    int entityId,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_CacheDataLoader_ValueType_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<long> GetEntityByIdAsync(
                    int entityId,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_CacheDataLoader_Nullable_ValueType_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<long?> GetEntityByIdAsync(
                    int entityId,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_GenericBatchDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, T>> GetEntityByIdAsync<T>(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_IDictionary_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Lookup_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader(Lookups = new string[] { nameof(CreateLookupKey) })]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;

                public static int CreateLookupKey(string key)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Lookup_From_OtherType_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader(Lookups = new string[] { nameof(CreateLookupKey) })]
                public static Task<IDictionary<int, Entity2>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;

                public static KeyValuePair<int, Entity2>? CreateLookupKey(Entity1 key)
                    => default!;
            }

            public class Entity1
            {
                public int Id { get; set; }

                public Entity2? Entity2 { get; set; }
            }

            public class Entity2
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_Nullable_Result_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Dictionary<int, string?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    /**
     * test specific for <see href="https://github.com/ChilliCream/graphql-platform/pull/8264"/>
     */
    [Fact]
    public async Task GenerateSource_BatchDataLoader_Nullable_Class_Instance_Result_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot([
            """
            namespace DataLoaderGen.Result;
            public class TestResultClass {}
            """,
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;
            using DataLoaderGen.Result;

            namespace TestNamespace.DataLoaderGen;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Dictionary<int, TestResultClass?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """
        ]).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Optional_State_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Dictionary<int, string?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    [DataLoaderState("key")] string? state,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Required_State_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Dictionary<int, string?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    [DataLoaderState("key")] string state,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_State_With_Default_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Dictionary<int, string?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    [DataLoaderState("key")] string state = "default",
                    CancellationToken cancellationToken = default)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DataLoader_With_Optional_Lookup()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader(Lookups = new string[] { nameof(CreateLookupKey) })]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;

                public static int? CreateLookupKey(Entity entity)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_PagingArguments_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;
            using GreenDonut.Data;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_SelectorBuilder_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    GreenDonut.Data.ISelectorBuilder selector,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_PredicateBuilder_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    GreenDonut.Data.IPredicateBuilder predicate,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_Without_Interface()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            [assembly: GreenDonut.DataLoaderDefaults(GenerateInterfaces = false)]

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    GreenDonut.Data.IPredicateBuilder predicate,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_With_QueryContext()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;
            using GreenDonut.Data;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    QueryContext<string> queryContext,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_With_SortDefinition()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;
            using GreenDonut.Data;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    SortDefinition<string> queryContext,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_DataLoader_Module_As_Internal()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            [assembly: GreenDonut.DataLoaderModule("Abc", IsInternal = true)]

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    GreenDonut.Data.IPredicateBuilder predicate,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_DataLoader_Module_As_Internal_Implementation_As_Internal()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            [assembly: GreenDonut.DataLoaderModule("Abc", IsInternal = true)]
            [assembly: GreenDonut.DataLoaderDefaults(
                AccessModifier = GreenDonut.DataLoaderAccessModifier.PublicInterface)]

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    GreenDonut.Data.IPredicateBuilder predicate,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_DataLoader_Module_As_Internal_Implementation_As_Internal_With_Group()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            [assembly: GreenDonut.DataLoaderModule("Abc", IsInternal = true)]
            [assembly: GreenDonut.DataLoaderDefaults(
                AccessModifier = GreenDonut.DataLoaderAccessModifier.PublicInterface)]

            namespace TestNamespace;

            [DataLoaderGroup("Test123")]
            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    GreenDonut.Data.IPredicateBuilder predicate,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generate_DataLoader_NullableAnnotated_AnonymousType_AsKey_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            public record Id1();
            public record Id2();
            public record Stuff();

            public class Dataloaders
            {
                [DataLoader]
                public static async Task<ILookup<(Id1, Id2?), Stuff>> GetStuff(
                    IReadOnlyList<(Id1, Id2?)> keys,
                    CancellationToken cancellationToken)
                {
                    await Task.CompletedTask;
                    return null!;
                }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_ReturnsNullableStruct_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, Entity?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public struct Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_GroupedDataLoader_ReturnsNullableStruct_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<ILookup<int, Entity?>> GetEntitiesByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public struct Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_MaxBatchSize_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader(MaxBatchSize = 2)]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GenerateSource_CacheDataLoader_With_MaxBatchSize_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader(MaxBatchSize = 2)]
                public static Task<Entity> GetEntityByIdAsync(
                    int entityId,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    private static GenericDataLoaderInfo? TryCreateGenericDataLoaderInfo(string source)
    {
        var compilation = TestHelper.CreateCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.Single();
        var methodSyntax = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        var attributeSyntax = methodSyntax.AttributeLists.SelectMany(t => t.Attributes).Single();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(methodSyntax)!;
        var attributeSymbol = (IMethodSymbol)semanticModel.GetSymbolInfo(attributeSyntax).Symbol!;
        var attributeData = methodSymbol.GetAttributes().Single();

        return GenericDataLoaderInfo.TryCreate(
                attributeSyntax,
                attributeSymbol,
                attributeData,
                methodSymbol,
                methodSyntax,
                compilation,
                out var dataLoaderInfo)
            ? dataLoaderInfo
            : null;
    }

    private static string CreateGenericDataLoaderSource(
        string declarations,
        string dataLoaderType,
        string method,
        string additionalDeclarations = "",
        string? attribute = null)
        => $$"""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using GreenDonut;

            {{additionalDeclarations}}

            namespace TestNamespace
            {
                internal static class Loaders
                {
                    {{declarations}}

                    [{{attribute ?? $"DataLoader<{dataLoaderType}>"}}]
                    public static {{method}}
                }
            }
            """;
}
