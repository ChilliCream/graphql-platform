using System.Reflection;
using System.Runtime.Loader;
using Basic.Reference.Assemblies;
using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types.Analyzers;
using HotChocolate.Types.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class SourceGeneratorBatchResolverTests
{
    [Fact]
    public async Task BatchResolver_Should_Bind_Argument_Per_Context_When_SourceGenerated()
    {
        // arrange
        // a source-generated [BatchResolver] with a GraphQL argument; the generator emits
        // contexts[i].ArgumentValue<string>("prefix"), so each aliased sibling must receive its
        // own argument value and each parent its own positional result.
        var assembly = CompileBatchAssembly(
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
        var result = await ExecuteSourceGeneratedAsync(
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
    public async Task BatchResolver_Should_Distribute_Results_When_ReturnTypeIsNotIList()
    {
        // arrange
        // REPRO: the generated batch delegate only distributes results when
        // "result is System.Collections.IList". A [BatchResolver] returning a non-IList shape
        // (here a LINQ IEnumerable<T>) leaves every context's Result null with no error. A default
        // user expects per-parent values just like the List<T> path.
        var assembly = CompileBatchAssembly(
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
        var result = await ExecuteSourceGeneratedAsync(
            assembly,
            """
            {
                brands {
                    name
                    label
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

    private static async Task<IExecutionResult> ExecuteSourceGeneratedAsync(
        Assembly assembly,
        string query)
    {
        var builder = new ServiceCollection().AddGraphQLServer(disableDefaultSecurity: true);

        var addModuleMethod = FindRegistrationMethod(
            assembly,
            m =>
            {
                var p = m.GetParameters();
                return m.Name.Equals("AddDemo", StringComparison.Ordinal)
                    && m.ReturnType == typeof(IRequestExecutorBuilder)
                    && p.Length == 1
                    && p[0].ParameterType == typeof(IRequestExecutorBuilder);
            });

        addModuleMethod.Invoke(null, [builder]);

        var executor = await builder.BuildRequestExecutorAsync();
        return await executor.ExecuteAsync(query);
    }

    private static MethodInfo FindRegistrationMethod(
        Assembly assembly,
        Func<MethodInfo, bool> predicate)
    {
        return assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: true, IsSealed: true }
                && t.Namespace == "Microsoft.Extensions.DependencyInjection")
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Single(predicate);
    }

    private static Assembly CompileBatchAssembly(string source, string assemblyName)
    {
        var parseOptions = CSharpParseOptions.Default;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        IEnumerable<PortableExecutableReference> references =
        [
#if NET8_0
            .. Net80.References.All,
#elif NET9_0
            .. Net90.References.All,
#elif NET10_0
            .. Net100.References.All,
#endif
            MetadataReference.CreateFromFile(typeof(ITypeSystemMember).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RequestDelegate).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RequestContext).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(HotChocolateExecutionSelectionExtensions).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IRequestExecutorBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ISelection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(QueryTypeAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Connection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PageConnection<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ISchemaDefinition).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IFeatureProvider).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(OperationType).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ParentAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(
                typeof(HotChocolateAspNetCoreServiceCollectionExtensions).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DataLoaderBase<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IDataLoader).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PagingArguments).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IPredicateBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DefaultPredicateBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IFilterContext).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(WebApplication).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            MetadataReference.CreateFromFile(
                typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Authorization.AuthorizeAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(UseOffsetPagingAttribute).Assembly.Location)
        ];

        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver
            .Create(new GraphQLServerGenerator())
            .RunGenerators(compilation);

        var generatedTrees = driver
            .GetRunResult()
            .Results
            .SelectMany(t => t.GeneratedSources)
            .Select(s => CSharpSyntaxTree.ParseText(s.SourceText, parseOptions, path: s.HintName));

        var updatedCompilation = compilation.AddSyntaxTrees(generatedTrees);

        using var stream = new MemoryStream();
        var emitResult = updatedCompilation.Emit(stream);

        if (!emitResult.Success)
        {
            throw new InvalidOperationException(
                string.Join(
                    Environment.NewLine,
                    emitResult.Diagnostics
                        .OrderBy(d => d.Severity)
                        .ThenBy(d => d.Id)
                        .Select(d => d.ToString())));
        }

        stream.Position = 0;

        var context = new AssemblyLoadContext(assemblyName, isCollectible: true);
        return context.LoadFromStream(stream);
    }
}
