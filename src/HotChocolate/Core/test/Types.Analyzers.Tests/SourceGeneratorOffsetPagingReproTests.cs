using System.Reflection;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class SourceGeneratorOffsetPagingReproTests
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

        var sourceGenerated = await SourceGeneratorTestHelpers.ExecuteWithSourceGeneratorRegistrationAsync(
            assembly,
            registrationMethodName: "AddDemo",
            query: "{ foo { key value } }");

        var addQueryType = await SourceGeneratorTestHelpers.ExecuteWithAddQueryTypeRegistrationAsync(
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

        var sourceGenerated = await SourceGeneratorTestHelpers.ExecuteWithSourceGeneratorRegistrationAsync(
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

        var addQueryAndMutationType = await SourceGeneratorTestHelpers.ExecuteWithAddQueryAndMutationTypeRegistrationAsync(
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

        var sourceGenerated = await SourceGeneratorTestHelpers.ExecuteWithSourceGeneratorRegistrationAsync(
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

        var addQueryType = await SourceGeneratorTestHelpers.ExecuteWithAddQueryTypeRegistrationAsync(
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

        var sourceGenerated = await SourceGeneratorTestHelpers.ExecuteWithSourceGeneratorRegistrationAsync(
            assembly,
            registrationMethodName: "AddDemo",
            query:
            """
            {
              foo
            }
            """,
            configureBuilder: static b => b.AddJsonTypeConverter());

        var addQueryType = await SourceGeneratorTestHelpers.ExecuteWithAddQueryTypeRegistrationAsync(
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

        var addTypesMethod = SourceGeneratorTestHelpers.FindRegistrationMethod(
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

        return SourceGeneratorTestHelpers.CompileReproAssembly(source, "SourceGeneratorOffsetPagingRepro");
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

        return SourceGeneratorTestHelpers.CompileReproAssembly(source, "SourceGeneratorDictionaryModuleRepro");
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

        return SourceGeneratorTestHelpers.CompileReproAssembly(
            source,
            "SourceGeneratorDictionaryMutationConventionsRepro");
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

        return SourceGeneratorTestHelpers.CompileReproAssembly(
            source,
            "SourceGeneratorOffsetPagingNullabilityRepro");
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

        return SourceGeneratorTestHelpers.CompileReproAssembly(source, "SourceGeneratorAnyTypeEscapingRepro");
    }
}
