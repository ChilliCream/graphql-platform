using System.Collections.Immutable;
using System.Text;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class MiddlewareModuleGenerator : IIncrementalGenerator
{
    private const string _namespace = "HotChocolate.Execution.Generated";
    
    private static readonly ISyntaxInspector[] _inspectors =
    [
        new TypeAttributeInspector(),
        new ClassBaseClassInspector(),
        new ModuleInspector(),
        new DataLoaderInspector(),
        new DataLoaderDefaultsInspector(),
        new OperationInspector(),
        new RequestMiddlewareInspector(),
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var modulesAndTypes =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsRelevant(s),
                    transform: TryGetModuleOrType)
                .Where(static t => t is not null)!
                .WithComparer(SyntaxInfoComparer.Default);

        var valueProvider = context.CompilationProvider.Combine(modulesAndTypes.Collect());

        context.RegisterSourceOutput(
            valueProvider,
            static (context, source) => Execute(context, source.Left, source.Right));
    }

    private static bool IsRelevant(SyntaxNode node)
        => IsMiddlewareMethod(node);

    private static bool IsMiddlewareMethod(SyntaxNode node)
        => node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: var method,
                },
            } &&
            (method.Equals("UseRequest") || method.Equals("UseField") || method.Equals("Use"));

    private static ISyntaxInfo? TryGetModuleOrType(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _inspectors.Length; i++)
        {
            if (_inspectors[i].TryHandle(context, out var syntaxInfo))
            {
                return syntaxInfo;
            }
        }

        return null;
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ISyntaxInfo> syntaxInfos)
    {
        if (syntaxInfos.IsEmpty)
        {
            return;
        }

        var module = syntaxInfos.GetModuleInfo(compilation.AssemblyName, out var defaultModule);
        var dataLoaderDefaults = syntaxInfos.GetDataLoaderDefaults();

        // if there is only the module info we do not need to generate a module.
        if (!defaultModule && syntaxInfos.Length == 1)
        {
            return;
        }

        var middlewareInfos = new List<RequestMiddlewareInfo>();

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is RequestMiddlewareInfo casted)
            {
                middlewareInfos.Add(casted);
            }
        }

        using var generator = new RequestMiddlewareSyntaxGenerator(
            module.ModuleName,
            "HotChocolate.Execution.Generated");
        
        generator.WriterHeader();
        generator.WriteBeginNamespace();
        
        generator.WriteBeginClass();
        
        for (var i = 0; i < middlewareInfos.Count; i++)
        {
            generator.WriteFactory(middlewareInfos[i], i);
            generator.WriteInterceptMethod(
                i,
                middlewareInfos[i].Location);
        }
        
        generator.WriteEndClass();
      
        generator.WriteEndNamespace();
        
        context.AddSource(WellKnownFileNames.MiddlewareFile, generator.ToSourceText());
    }
}