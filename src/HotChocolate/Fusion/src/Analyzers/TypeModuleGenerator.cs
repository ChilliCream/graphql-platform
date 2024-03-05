using System.Collections.Immutable;
using HotChocolate.Fusion.Analyzers.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class TypeModuleGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var modulesAndTypes =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsRelevant(s),
                    transform: TryGetProjectClass)
                .Where(static t => t is not null);

        var valueProvider = context.CompilationProvider.Combine(modulesAndTypes.Collect());

        context.RegisterSourceOutput(
            valueProvider,
            static (context, source) => Execute(context, source.Left, source.Right!));
            
    }
    
    private static bool IsRelevant(SyntaxNode node)
        => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0, };

    private ProjectClass? TryGetProjectClass(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        => new ProjectClass("abc");

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ProjectClass> syntaxInfos)
    {
        context.AddSource("FusionGatewayConfiguration.g.cs", Resources.CliCode);
    }
}