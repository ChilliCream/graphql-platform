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

public class SourceGeneratorOffsetPagingReproTests
{
    [Fact]
    public async Task QueryType_SourceGenerator_Path_Works_Like_AddQueryType_Path()
    {
        var assembly = CompileReproAssembly();

        var sourceGeneratorException = await BuildSchemaWithSourceGeneratorRegistrationAsync(assembly);
        var addQueryTypeException = await BuildSchemaWithAddQueryTypeRegistrationAsync(assembly);

        Assert.Null(sourceGeneratorException);
        Assert.Null(addQueryTypeException);
    }

    private static async Task<Exception?> BuildSchemaWithSourceGeneratorRegistrationAsync(Assembly assembly)
    {
        var services = new ServiceCollection();
        var builder = services.AddGraphQLServer(disableDefaultSecurity: true);

        var addTypesMethod = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: true, IsSealed: true }
                && t.Namespace == "Microsoft.Extensions.DependencyInjection")
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Single(m =>
            {
                var p = m.GetParameters();
                return m.Name.StartsWith("Add", StringComparison.Ordinal)
                    && m.Name.EndsWith("Types", StringComparison.Ordinal)
                    && m.ReturnType == typeof(IRequestExecutorBuilder)
                    && p.Length == 1
                    && p[0].ParameterType == typeof(IRequestExecutorBuilder);
            });

        addTypesMethod.Invoke(null, [builder]);

        return await Record.ExceptionAsync(
            async () => await builder.BuildSchemaAsync());
    }

    private static async Task<Exception?> BuildSchemaWithAddQueryTypeRegistrationAsync(Assembly assembly)
    {
        var runtimeQueryType = assembly.GetType("Repro.RuntimeQuery")
            ?? throw new InvalidOperationException("Could not locate runtime query type.");

        var builder = new ServiceCollection()
            .AddGraphQLServer(disableDefaultSecurity: true)
            .AddQueryType(runtimeQueryType);

        return await Record.ExceptionAsync(
            async () => await builder.BuildSchemaAsync());
    }

    private static Assembly CompileReproAssembly()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using HotChocolate.Types;

            namespace Repro;

            [QueryType]
            public static partial class SourceGeneratedQuery
            {
                [UseOffsetPaging]
                public static async Task<Dictionary<string, string>> UglyLegacyResolver()
                {
                    await Task.Yield();
                    return new();
                }
            }

            public class RuntimeQuery
            {
                [UseOffsetPaging]
                public async Task<Dictionary<string, string>> UglyLegacyResolver()
                {
                    await Task.Yield();
                    return new();
                }
            }
            """;

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
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Authorization.AuthorizeAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(UseOffsetPagingAttribute).Assembly.Location)
        ];

        var compilation = CSharpCompilation.Create(
            assemblyName: "SourceGeneratorOffsetPagingRepro",
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
            .Select(s => CSharpSyntaxTree.ParseText(
                s.SourceText,
                parseOptions,
                path: s.HintName));

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

        var context = new AssemblyLoadContext("SourceGeneratorOffsetPagingRepro", isCollectible: true);
        return context.LoadFromStream(stream);
    }
}
