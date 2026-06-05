using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

/// <summary>
/// A reusable harness for end-to-end source-generator tests. It compiles one or more
/// in-memory assemblies, runs the <c>HotChocolate.Types.Analyzers</c> generator over each
/// of them, emits and loads the result into a collectible assembly load context, discovers
/// the generated registration method by reflection, and wires it onto a real
/// <see cref="IRequestExecutorBuilder"/> so a scenario can be executed or its schema
/// inspected at runtime.
/// </summary>
/// <remarks>
/// Each scenario is expressed as one or more C# source strings instead of a dedicated test
/// project. The compiled assemblies reference the same loaded HotChocolate assemblies as the
/// test process (see <see cref="GeneratorReferences"/>), so the generated registration code
/// binds to the test process's <see cref="IRequestExecutorBuilder"/> and the reflective
/// invocation composes with the rest of the GraphQL configuration.
/// </remarks>
internal static class GeneratorTestServer
{
    /// <summary>
    /// Compiles the supplied source(s) into a single in-memory assembly, runs the generator,
    /// loads the result, and builds an <see cref="IRequestExecutor"/> with the generated
    /// registration method applied.
    /// </summary>
    /// <param name="source">The C# source defining the scenario.</param>
    /// <param name="configure">
    /// An optional callback for arbitrary builder chaining (for example
    /// <c>AddMutationConventions</c>, <c>ModifyPagingOptions</c>, or <c>AddTypeExtension</c>).
    /// </param>
    /// <param name="registrationMethodName">
    /// The explicit name of the generated registration method (for example <c>AddDemo</c>).
    /// When omitted, the single generated <c>Add{Module}</c> method is discovered automatically.
    /// </param>
    /// <param name="disableDefaultSecurity">
    /// Whether to disable the default security configuration. Defaults to <see langword="true"/>.
    /// </param>
    /// <param name="assemblyName">An optional name for the generated assembly.</param>
    public static Task<IRequestExecutor> CreateExecutorAsync(
        [StringSyntax("csharp")] string source,
        Action<IRequestExecutorBuilder>? configure = null,
        string? registrationMethodName = null,
        bool disableDefaultSecurity = true,
        string? assemblyName = null)
        => CreateExecutorAsync(
            [source],
            configure,
            registrationMethodName,
            disableDefaultSecurity,
            assemblyName);

    /// <summary>
    /// Compiles the supplied sources into a single in-memory assembly, runs the generator,
    /// loads the result, and builds an <see cref="IRequestExecutor"/> with the generated
    /// registration method applied.
    /// </summary>
    public static async Task<IRequestExecutor> CreateExecutorAsync(
        string[] sources,
        Action<IRequestExecutorBuilder>? configure = null,
        string? registrationMethodName = null,
        bool disableDefaultSecurity = true,
        string? assemblyName = null)
    {
        var assembly = CompileAndLoad(sources, assemblyName ?? CreateAssemblyName());
        var builder = CreateBuilder(disableDefaultSecurity);
        InvokeRegistration(builder, assembly, registrationMethodName);
        configure?.Invoke(builder);
        return await builder.BuildRequestExecutorAsync();
    }

    /// <summary>
    /// Compiles the supplied source(s) into a single in-memory assembly, runs the generator,
    /// loads the result, and builds the schema with the generated registration method applied.
    /// </summary>
    public static Task<ISchemaDefinition> CreateSchemaAsync(
        [StringSyntax("csharp")] string source,
        Action<IRequestExecutorBuilder>? configure = null,
        string? registrationMethodName = null,
        bool disableDefaultSecurity = true,
        string? assemblyName = null)
        => CreateSchemaAsync(
            [source],
            configure,
            registrationMethodName,
            disableDefaultSecurity,
            assemblyName);

    /// <summary>
    /// Compiles the supplied sources into a single in-memory assembly, runs the generator,
    /// loads the result, and builds the schema with the generated registration method applied.
    /// </summary>
    public static async Task<ISchemaDefinition> CreateSchemaAsync(
        string[] sources,
        Action<IRequestExecutorBuilder>? configure = null,
        string? registrationMethodName = null,
        bool disableDefaultSecurity = true,
        string? assemblyName = null)
    {
        var assembly = CompileAndLoad(sources, assemblyName ?? CreateAssemblyName());
        var builder = CreateBuilder(disableDefaultSecurity);
        InvokeRegistration(builder, assembly, registrationMethodName);
        configure?.Invoke(builder);
        return await builder.BuildSchemaAsync();
    }

    /// <summary>
    /// Compiles a set of in-memory assemblies that may reference one another, runs the
    /// generator over each, and builds the schema with every generated registration method
    /// applied to the same builder (in the declared order). Use this to replace satellite
    /// test projects with in-memory compilations for cross-assembly scenarios.
    /// </summary>
    /// <param name="assemblies">
    /// The assemblies to compile, in dependency order. An assembly may reference any assembly
    /// declared before it through <see cref="GeneratorAssembly.References"/>.
    /// </param>
    /// <param name="configure">An optional callback for arbitrary builder chaining.</param>
    /// <param name="disableDefaultSecurity">
    /// Whether to disable the default security configuration. Defaults to <see langword="true"/>.
    /// </param>
    public static async Task<ISchemaDefinition> CreateSchemaAsync(
        IReadOnlyList<GeneratorAssembly> assemblies,
        Action<IRequestExecutorBuilder>? configure = null,
        bool disableDefaultSecurity = true)
    {
        var loaded = CompileAndLoad(assemblies);
        var builder = CreateBuilder(disableDefaultSecurity);

        foreach (var (spec, assembly) in loaded)
        {
            if (!spec.Register)
            {
                continue;
            }

            InvokeRegistration(builder, assembly, spec.RegistrationMethodName);
        }

        configure?.Invoke(builder);
        return await builder.BuildSchemaAsync();
    }

    /// <summary>
    /// Compiles a set of in-memory assemblies that may reference one another, runs the
    /// generator over each, and builds an <see cref="IRequestExecutor"/> with every generated
    /// registration method applied to the same builder (in the declared order).
    /// </summary>
    public static async Task<IRequestExecutor> CreateExecutorAsync(
        IReadOnlyList<GeneratorAssembly> assemblies,
        Action<IRequestExecutorBuilder>? configure = null,
        bool disableDefaultSecurity = true)
    {
        var loaded = CompileAndLoad(assemblies);
        var builder = CreateBuilder(disableDefaultSecurity);

        foreach (var (spec, assembly) in loaded)
        {
            if (!spec.Register)
            {
                continue;
            }

            InvokeRegistration(builder, assembly, spec.RegistrationMethodName);
        }

        configure?.Invoke(builder);
        return await builder.BuildRequestExecutorAsync();
    }

    /// <summary>
    /// Compiles the supplied source into a single in-memory assembly, runs the generator over
    /// it, and loads the emitted assembly. Use this when a test needs the loaded assembly to
    /// build more than one registration path (for example the generated module and a
    /// hand-rolled runtime type) from the same compiled types.
    /// </summary>
    public static Assembly Compile([StringSyntax("csharp")] string source, string? assemblyName = null)
        => CompileAndLoad([source], assemblyName ?? CreateAssemblyName());

    /// <summary>
    /// Applies a generated registration method from the supplied assembly to the builder. When
    /// <paramref name="registrationMethodName"/> is omitted, the single generated
    /// <c>Add{Module}</c> method is discovered automatically.
    /// </summary>
    public static void ApplyGeneratedRegistration(
        IRequestExecutorBuilder builder,
        Assembly assembly,
        string? registrationMethodName = null)
        => InvokeRegistration(builder, assembly, registrationMethodName);

    private static IRequestExecutorBuilder CreateBuilder(bool disableDefaultSecurity)
        => new ServiceCollection().AddGraphQLServer(disableDefaultSecurity: disableDefaultSecurity);

    private static void InvokeRegistration(
        IRequestExecutorBuilder builder,
        Assembly assembly,
        string? registrationMethodName)
    {
        var method = FindRegistrationMethod(assembly, registrationMethodName);
        method.Invoke(null, [builder]);
    }

    private static MethodInfo FindRegistrationMethod(Assembly assembly, string? registrationMethodName)
    {
        var candidates = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: true, IsSealed: true }
                && t.Namespace == "Microsoft.Extensions.DependencyInjection")
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m =>
            {
                var parameters = m.GetParameters();
                return m.ReturnType == typeof(IRequestExecutorBuilder)
                    && parameters.Length == 1
                    && parameters[0].ParameterType == typeof(IRequestExecutorBuilder)
                    && (registrationMethodName is null
                        ? m.Name.StartsWith("Add", StringComparison.Ordinal)
                        : m.Name.Equals(registrationMethodName, StringComparison.Ordinal));
            })
            .ToArray();

        return candidates.Length switch
        {
            1 => candidates[0],
            0 => throw new InvalidOperationException(
                registrationMethodName is null
                    ? $"No generated registration method was found in assembly '{assembly.GetName().Name}'."
                    : $"No generated registration method named '{registrationMethodName}' was found "
                        + $"in assembly '{assembly.GetName().Name}'."),
            _ => throw new InvalidOperationException(
                "Multiple generated registration methods were found in assembly "
                    + $"'{assembly.GetName().Name}' ({string.Join(", ", candidates.Select(m => m.Name))}). "
                    + "Specify an explicit registration method name to disambiguate.")
        };
    }

    private static Assembly CompileAndLoad(string[] sources, string assemblyName)
    {
        var compilation = CreateCompilation(sources, assemblyName, references: []);
        var (stream, _) = EmitToStream(compilation);
        return LoadFromStream(stream, assemblyName);
    }

    private static IReadOnlyList<(GeneratorAssembly Spec, Assembly Assembly)> CompileAndLoad(
        IReadOnlyList<GeneratorAssembly> assemblies)
    {
        var emittedReferences = new Dictionary<string, MetadataReference>(StringComparer.Ordinal);
        var results = new List<(GeneratorAssembly, Assembly)>(assemblies.Count);

        // All assemblies of a multi-assembly scenario share a single collectible context so
        // that cross-assembly references between them resolve against the loaded members.
        var context = new AssemblyLoadContext(CreateAssemblyName(), isCollectible: true);

        foreach (var spec in assemblies)
        {
            var dependencyReferences = new List<MetadataReference>(spec.References.Count);
            foreach (var dependency in spec.References)
            {
                if (!emittedReferences.TryGetValue(dependency, out var reference))
                {
                    throw new InvalidOperationException(
                        $"Assembly '{spec.AssemblyName}' references '{dependency}', "
                            + "which has not been declared earlier in the list.");
                }

                dependencyReferences.Add(reference);
            }

            var compilation = CreateCompilation(spec.Sources, spec.AssemblyName, dependencyReferences);
            var (stream, image) = EmitToStream(compilation);

            // Make the (post-generation) image available to assemblies declared later so that
            // cross-assembly references resolve against the generated members as well.
            emittedReferences[spec.AssemblyName] = MetadataReference.CreateFromImage(image);

            using (stream)
            {
                results.Add((spec, context.LoadFromStream(stream)));
            }
        }

        return results;
    }

    private static CSharpCompilation CreateCompilation(
        string[] sources,
        string assemblyName,
        IReadOnlyList<MetadataReference> references)
    {
        var parseOptions = CSharpParseOptions.Default;
        var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s, parseOptions)).ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: syntaxTrees,
            references: [.. GeneratorReferences.All, .. references],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver
            .Create(new Analyzers.GraphQLServerGenerator())
            .RunGenerators(compilation);

        var generatedTrees = driver
            .GetRunResult()
            .Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => CSharpSyntaxTree.ParseText(s.SourceText, parseOptions, path: s.HintName));

        return compilation.AddSyntaxTrees(generatedTrees);
    }

    private static (MemoryStream Stream, byte[] Image) EmitToStream(CSharpCompilation compilation)
    {
        var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);

        if (!emitResult.Success)
        {
            stream.Dispose();
            throw new InvalidOperationException(
                "Failed to emit the in-memory assembly:"
                    + Environment.NewLine
                    + string.Join(
                        Environment.NewLine,
                        emitResult.Diagnostics
                            .Where(d => d.Severity == DiagnosticSeverity.Error)
                            .OrderBy(d => d.Id)
                            .Select(d => d.ToString())));
        }

        stream.Position = 0;
        return (stream, stream.ToArray());
    }

    private static Assembly LoadFromStream(MemoryStream stream, string assemblyName)
    {
        using (stream)
        {
            stream.Position = 0;
            var context = new AssemblyLoadContext(assemblyName, isCollectible: true);
            return context.LoadFromStream(stream);
        }
    }

    private static string CreateAssemblyName()
        => $"GeneratorTestServer_{Guid.NewGuid():N}";
}

/// <summary>
/// Describes a single in-memory assembly in a multi-assembly source-generator scenario.
/// </summary>
/// <param name="AssemblyName">
/// The unique name of the assembly. References to this assembly from later assemblies are
/// matched by this name.
/// </param>
/// <param name="Sources">The C# source(s) that make up the assembly.</param>
/// <param name="References">
/// The names of assemblies (declared earlier in the scenario) that this assembly references.
/// </param>
/// <param name="RegistrationMethodName">
/// The explicit name of the generated registration method, or <see langword="null"/> to
/// discover the single generated method automatically.
/// </param>
/// <param name="Register">
/// Whether the assembly's generated registration method should be applied to the builder.
/// Set to <see langword="false"/> for a dependency-only assembly that contributes no GraphQL
/// types of its own (for example one that only defines shared runtime types referenced by
/// other assemblies). The assembly is still compiled and loaded so those references resolve.
/// </param>
internal sealed record GeneratorAssembly(
    string AssemblyName,
    string[] Sources,
    IReadOnlyList<string> References,
    string? RegistrationMethodName = null,
    bool Register = true)
{
    /// <summary>
    /// Creates an assembly spec from a single source string with no cross-assembly references.
    /// </summary>
    public GeneratorAssembly(
        string assemblyName,
        [StringSyntax("csharp")] string source,
        string? registrationMethodName = null)
        : this(assemblyName, [source], [], registrationMethodName)
    {
    }
}
