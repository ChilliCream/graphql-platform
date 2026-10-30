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

internal static class SourceGeneratorTestHelpers
{
    internal static async Task<ExecutionResult> ExecuteWithSourceGeneratorRegistrationAsync(
        Assembly assembly,
        string registrationMethodName,
        string query,
        Action<IRequestExecutorBuilder>? configureBuilder = null)
    {
        var builder = new ServiceCollection().AddGraphQLServer(disableDefaultSecurity: true);

        var addModuleMethod = FindRegistrationMethod(
            assembly,
            m =>
            {
                var p = m.GetParameters();
                return m.Name.Equals(registrationMethodName, StringComparison.Ordinal)
                    && m.ReturnType == typeof(IRequestExecutorBuilder)
                    && p.Length == 1
                    && p[0].ParameterType == typeof(IRequestExecutorBuilder);
            });

        addModuleMethod.Invoke(null, [builder]);
        configureBuilder?.Invoke(builder);

        var executor = await builder.BuildRequestExecutorAsync();
        var result = await executor.ExecuteAsync(query);
        return new ExecutionResult(executor.Schema.ToString(), result.ToJson());
    }

    internal static async Task<ExecutionResult> ExecuteWithAddQueryTypeRegistrationAsync(
        Assembly assembly,
        string runtimeQueryTypeName,
        string query,
        Action<IRequestExecutorBuilder>? configureBuilder = null)
    {
        var runtimeQueryType = assembly.GetType(runtimeQueryTypeName)
            ?? throw new InvalidOperationException("Could not locate runtime query type.");

        var builder = new ServiceCollection()
            .AddGraphQLServer(disableDefaultSecurity: true)
            .AddQueryType(runtimeQueryType);
        configureBuilder?.Invoke(builder);

        var executor = await builder.BuildRequestExecutorAsync();
        var result = await executor.ExecuteAsync(query);
        return new ExecutionResult(executor.Schema.ToString(), result.ToJson());
    }

    internal static async Task<ExecutionResult> ExecuteWithAddQueryAndMutationTypeRegistrationAsync(
        Assembly assembly,
        string runtimeQueryTypeName,
        string runtimeMutationTypeName,
        string query,
        Action<IRequestExecutorBuilder>? configureBuilder = null)
    {
        var runtimeQueryType = assembly.GetType(runtimeQueryTypeName)
            ?? throw new InvalidOperationException("Could not locate runtime query type.");
        var runtimeMutationType = assembly.GetType(runtimeMutationTypeName)
            ?? throw new InvalidOperationException("Could not locate runtime mutation type.");

        var builder = new ServiceCollection()
            .AddGraphQLServer(disableDefaultSecurity: true)
            .AddQueryType(runtimeQueryType)
            .AddMutationType(runtimeMutationType);
        configureBuilder?.Invoke(builder);

        var executor = await builder.BuildRequestExecutorAsync();
        var result = await executor.ExecuteAsync(query);
        return new ExecutionResult(executor.Schema.ToString(), result.ToJson());
    }

    internal static MethodInfo FindRegistrationMethod(
        Assembly assembly,
        Func<MethodInfo, bool> predicate)
    {
        return assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: true, IsSealed: true, Namespace: "Microsoft.Extensions.DependencyInjection" })
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Single(predicate);
    }

    internal static Assembly CompileReproAssembly(string source, string assemblyName)
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
#elif NET11_0
            .. Net110.References.All,
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
            MetadataReference.CreateFromFile(typeof(HotChocolateAspNetCoreServiceCollectionExtensions).Assembly.Location),
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

        var context = new AssemblyLoadContext(assemblyName, isCollectible: true);
        return context.LoadFromStream(stream);
    }

    internal sealed record ExecutionResult(string Schema, string Result);
}
