using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
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

public class SourceGeneratorReproTests
{
    [Fact]
    public async Task QueryType_SourceGenerator_Path_Works_Like_AddQueryType_Path()
    {
        var assembly = CompileOffsetPagingReproAssembly();

        var sourceGeneratorException = await BuildSchemaWithSourceGeneratorRegistrationAsync(assembly);
        var addQueryTypeException = await BuildSchemaWithAddQueryTypeRegistrationAsync(assembly);

        Assert.Null(sourceGeneratorException);
        Assert.Null(addQueryTypeException);
    }

    [Fact]
    public async Task Module_QueryType_Dictionary_Result_SourceGenerator_Path_Works_Like_AddQueryType_Path()
    {
        var assembly = CompileModuleDictionaryReproAssembly();

        var sourceGenerated = await ExecuteWithSourceGeneratorRegistrationAsync(
            assembly,
            registrationMethodName: "AddDemo",
            query: "{ foo { key value } }");

        var addQueryType = await ExecuteWithAddQueryTypeRegistrationAsync(
            assembly,
            runtimeQueryTypeName: "Repro.RuntimeQuery",
            query: "{ foo { key value } }");

        Assert.Contains("foo: [KeyValuePairOfStringAndString!]!", sourceGenerated.Schema);
        Assert.Equal(addQueryType.Result, sourceGenerated.Result);
        Assert.DoesNotContain("\"errors\"", sourceGenerated.Result, StringComparison.Ordinal);
        Assert.Contains("\"key\": \"foo\"", sourceGenerated.Result, StringComparison.Ordinal);
        Assert.Contains("\"value\": \"bar\"", sourceGenerated.Result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Module_Dictionary_MutationConventions_Input_Accepts_KeyValuePair_When_Query_Has_Dictionary_Output()
    {
        var assembly = CompileModuleDictionaryMutationConventionsReproAssembly();

        var sourceGenerated = await ExecuteWithSourceGeneratorRegistrationAsync(
            assembly,
            registrationMethodName: "AddDemo",
            query:
            """
            mutation m {
              patchUserSettings(
                input: {
                  settings: [{ key: "open-workspace", value: "applications" }]
                }
              ) {
                keyValuePairOfStringAndString {
                  key
                  value
                }
              }
            }
            """,
            configureBuilder: static b => b.AddMutationConventions());

        var addQueryAndMutationType = await ExecuteWithAddQueryAndMutationTypeRegistrationAsync(
            assembly,
            runtimeQueryTypeName: "Repro.RuntimeQuery",
            runtimeMutationTypeName: "Repro.RuntimeMutation",
            query:
            """
            mutation m {
              patchUserSettings(
                input: {
                  settings: [{ key: "open-workspace", value: "applications" }]
                }
              ) {
                keyValuePairOfStringAndString {
                  key
                  value
                }
              }
            }
            """,
            configureBuilder: static b => b.AddMutationConventions());

        Assert.Equal(addQueryAndMutationType.Result, sourceGenerated.Result);
        Assert.DoesNotContain("\"errors\"", sourceGenerated.Result, StringComparison.Ordinal);
        Assert.Contains("\"key\": \"open-workspace\"", sourceGenerated.Result, StringComparison.Ordinal);
        Assert.Contains("\"value\": \"applications\"", sourceGenerated.Result, StringComparison.Ordinal);
        Assert.Contains("type KeyValuePairOfStringAndString", sourceGenerated.Schema, StringComparison.Ordinal);
        Assert.Contains("input KeyValuePairOfStringAndStringInput", sourceGenerated.Schema, StringComparison.Ordinal);
        Assert.Contains("key: String!", sourceGenerated.Schema, StringComparison.Ordinal);
        Assert.Contains("value: String!", sourceGenerated.Schema, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Module_OffsetPaging_TaskIEnumerableOfNonNullReferenceType_Infers_NonNull_Items()
    {
        var assembly = CompileModuleOffsetPagingNullabilityReproAssembly();

        var sourceGenerated = await ExecuteWithSourceGeneratorRegistrationAsync(
            assembly,
            registrationMethodName: "AddDemo",
            query:
            """
            {
              foos {
                items {
                  bar
                }
              }
            }
            """);

        var addQueryType = await ExecuteWithAddQueryTypeRegistrationAsync(
            assembly,
            runtimeQueryTypeName: "Repro.RuntimeQuery",
            query:
            """
            {
              foos {
                items {
                  bar
                }
              }
            }
            """);

        Assert.Contains("type FoosCollectionSegment", sourceGenerated.Schema, StringComparison.Ordinal);
        Assert.Contains("items: [Foo!]", sourceGenerated.Schema, StringComparison.Ordinal);
        Assert.Equal(addQueryType.Result, sourceGenerated.Result);
    }

    [Fact]
    public async Task Module_AnyType_Output_Does_Not_Double_Escape_Json_Escape_Sequences()
    {
        var assembly = CompileModuleAnyTypeEscapingReproAssembly();

        var sourceGenerated = await ExecuteWithSourceGeneratorRegistrationAsync(
            assembly,
            registrationMethodName: "AddDemo",
            query:
            """
            {
              foo
            }
            """,
            configureBuilder: static b => b.AddJsonTypeConverter());

        var addQueryType = await ExecuteWithAddQueryTypeRegistrationAsync(
            assembly,
            runtimeQueryTypeName: "Repro.RuntimeQuery",
            query:
            """
            {
              foo
            }
            """,
            configureBuilder: static b => b.AddJsonTypeConverter());

        using var sourceGeneratedJson = JsonDocument.Parse(sourceGenerated.Result);
        using var addQueryTypeJson = JsonDocument.Parse(addQueryType.Result);

        var sourceGeneratedDescription = sourceGeneratedJson.RootElement
            .GetProperty("data")
            .GetProperty("foo")
            .GetProperty("description")
            .GetString();

        var addQueryTypeDescription = addQueryTypeJson.RootElement
            .GetProperty("data")
            .GetProperty("foo")
            .GetProperty("description")
            .GetString();

        Assert.Equal("Special char: ü", sourceGeneratedDescription);
        Assert.Equal("Special char: ü", addQueryTypeDescription);
    }

    private static async Task<Exception?> BuildSchemaWithSourceGeneratorRegistrationAsync(Assembly assembly)
    {
        var services = new ServiceCollection();
        var builder = services.AddGraphQLServer(disableDefaultSecurity: true);

        var addTypesMethod = FindRegistrationMethod(
            assembly,
            m =>
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

    private static async Task<ExecutionResult> ExecuteWithSourceGeneratorRegistrationAsync(
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

    private static async Task<ExecutionResult> ExecuteWithAddQueryTypeRegistrationAsync(
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

    private static async Task<ExecutionResult> ExecuteWithAddQueryAndMutationTypeRegistrationAsync(
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

    [Fact]
    public async Task Module_SourceGen_CustomParameterExpressionBuilder_IsInjected_NotAnArgument()
    {
        var assembly = CompileCustomParameterReproAssembly();

        var result = await ExecuteWithSourceGeneratorRegistrationAsync(
            assembly,
            registrationMethodName: "AddDemo",
            query: "mutation { doThing(name: \"!\") }",
            configureBuilder: static b =>
                b.AddParameterExpressionBuilder(_ => new InjectedUser { Name = "injected" }));

        // the custom parameter is not exposed as a GraphQL argument (only 'name' is)
        Assert.Contains("doThing(name: String!): String!", result.Schema, StringComparison.Ordinal);

        // the custom parameter value is injected into the resolver
        using var json = JsonDocument.Parse(result.Result);
        var value = json.RootElement.GetProperty("data").GetProperty("doThing").GetString();
        Assert.Equal("injected!", value);
    }

    [Fact]
    public async Task Module_SourceGen_CustomPredicateBuilder_AssignableInterfaceParameter_FailsLoud()
    {
        var assembly = CompileInterfaceParameterReproAssembly();

        var builder = new ServiceCollection().AddGraphQLServer(disableDefaultSecurity: true);
        var addModule = FindRegistrationMethod(
            assembly,
            m =>
            {
                var p = m.GetParameters();
                return m.Name.Equals("AddDemo", StringComparison.Ordinal)
                    && m.ReturnType == typeof(IRequestExecutorBuilder)
                    && p.Length == 1
                    && p[0].ParameterType == typeof(IRequestExecutorBuilder);
            });
        addModule.Invoke(null, [builder]);

        // a custom ParameterInfo predicate whose value type (ConcreteInjectedUser) is assignable to
        // the resolver parameter type (IInjectedUser). The source generator cannot evaluate the
        // predicate, so the build must fail with a clear error instead of misbinding the parameter.
        builder.AddParameterExpressionBuilder(
            _ => new ConcreteInjectedUser(),
            p => p.Name == "user");

        var exception = await Record.ExceptionAsync(
            async () => await builder.BuildSchemaAsync(
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.IsType<SchemaException>(exception);
        Assert.Contains(
            "custom parameter expression builder",
            exception.ToString(),
            StringComparison.Ordinal);
    }

    private static Assembly CompileInterfaceParameterReproAssembly()
    {
        const string source = """
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            [QueryType]
            public static partial class SourceGeneratedQuery
            {
                public static string Ping() => "pong";
            }

            [MutationType]
            public static partial class SourceGeneratedMutation
            {
                public static string DoThing(IInjectedUser user, string name)
                    => user.Name + name;
            }
            """;

        return CompileReproAssembly(
            source,
            "SourceGeneratorInterfaceParameterRepro",
            [MetadataReference.CreateFromFile(typeof(IInjectedUser).Assembly.Location)]);
    }

    private static Assembly CompileCustomParameterReproAssembly()
    {
        const string source = """
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            [QueryType]
            public static partial class SourceGeneratedQuery
            {
                public static string Ping() => "pong";
            }

            [MutationType]
            public static partial class SourceGeneratedMutation
            {
                public static string DoThing(InjectedUser user, string name)
                    => user.Name + name;
            }
            """;

        return CompileReproAssembly(
            source,
            "SourceGeneratorCustomParameterRepro",
            [MetadataReference.CreateFromFile(typeof(InjectedUser).Assembly.Location)]);
    }

    private static Assembly CompileOffsetPagingReproAssembly()
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

        return CompileReproAssembly(source, "SourceGeneratorOffsetPagingRepro");
    }

    private static Assembly CompileModuleDictionaryReproAssembly()
    {
        const string source = """
            using System.Collections.Generic;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            [QueryType]
            public static partial class SourceGeneratedQuery
            {
                public static Dictionary<string, string> Foo()
                    => new()
                    {
                        ["foo"] = "bar"
                    };
            }

            public class RuntimeQuery
            {
                public Dictionary<string, string> Foo()
                    => new()
                    {
                        ["foo"] = "bar"
                    };
            }
            """;

        return CompileReproAssembly(source, "SourceGeneratorDictionaryModuleRepro");
    }

    private static Assembly CompileModuleDictionaryMutationConventionsReproAssembly()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            [MutationType]
            public static partial class SourceGeneratedMutation
            {
                public static async Task<Dictionary<string, string>?> PatchUserSettingsAsync(
                    Dictionary<string, string> settings)
                {
                    await Task.Yield();
                    return settings;
                }
            }

            [QueryType]
            public static partial class SourceGeneratedQuery
            {
                public static async Task<Dictionary<string, string>> GetUserSettingsAsync(
                    List<string> settingIdentifiers)
                {
                    await Task.Yield();
                    return new();
                }
            }

            public class RuntimeMutation
            {
                public async Task<Dictionary<string, string>?> PatchUserSettingsAsync(
                    Dictionary<string, string> settings)
                {
                    await Task.Yield();
                    return settings;
                }
            }

            public class RuntimeQuery
            {
                public async Task<Dictionary<string, string>> GetUserSettingsAsync(
                    List<string> settingIdentifiers)
                {
                    await Task.Yield();
                    return new();
                }
            }
            """;

        return CompileReproAssembly(source, "SourceGeneratorDictionaryMutationConventionsRepro");
    }

    private static Assembly CompileModuleOffsetPagingNullabilityReproAssembly()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            [QueryType]
            public static partial class SourceGeneratedQuery
            {
                [UseOffsetPaging]
                public static async Task<IEnumerable<Foo>> GetFoos()
                {
                    await Task.Yield();
                    return [];
                }
            }

            public class RuntimeQuery
            {
                [UseOffsetPaging]
                public async Task<IEnumerable<Foo>> GetFoos()
                {
                    await Task.Yield();
                    return [];
                }
            }

            public record Foo(string Bar);
            """;

        return CompileReproAssembly(source, "SourceGeneratorOffsetPagingNullabilityRepro");
    }

    private static Assembly CompileModuleAnyTypeEscapingReproAssembly()
    {
        const string source = """
            using HotChocolate;
            using HotChocolate.Types;

            [assembly: Module("Demo")]

            namespace Repro;

            [QueryType]
            public static partial class SourceGeneratedQuery
            {
                [GraphQLType<AnyType>]
                public static object GetFoo()
                    => new Foo("Special char: ü");
            }

            public class RuntimeQuery
            {
                [GraphQLType<AnyType>]
                public object GetFoo()
                    => new Foo("Special char: ü");
            }

            public record Foo(string Description);
            """;

        return CompileReproAssembly(source, "SourceGeneratorAnyTypeEscapingRepro");
    }

    private static Assembly CompileReproAssembly(
        string source,
        string assemblyName,
        IEnumerable<PortableExecutableReference>? extraReferences = null)
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
            MetadataReference.CreateFromFile(typeof(UseOffsetPagingAttribute).Assembly.Location),
            .. extraReferences ?? []
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

    private sealed record ExecutionResult(string Schema, string Result);
}

// Shared with the source-generated repro assembly (must be a compile-time type so the test can register
// AddParameterExpressionBuilder<InjectedUser>). Supplied via a custom parameter expression builder,
// never a GraphQL argument.
public sealed class InjectedUser
{
    public string Name { get; set; } = string.Empty;
}

// Interface + implementation shared with the source-generated repro assembly. Used to verify that a
// custom ParameterInfo predicate whose value type is assignable to an interface/base parameter type is
// detected by the source-generator binding path.
public interface IInjectedUser
{
    string Name { get; }
}

public sealed class ConcreteInjectedUser : IInjectedUser
{
    public string Name { get; set; } = string.Empty;
}
