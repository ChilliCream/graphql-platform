using System.Collections.Immutable;
using HotChocolate.Fusion.Composition.Analyzers.Helpers;
using HotChocolate.Fusion.Composition.Analyzers.Models;
using HotChocolate.Fusion.Composition.Analyzers.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Fusion.Composition.Analyzers;

[Generator]
public class ConfigurationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(AddInitializationContent);

        var modulesAndTypes =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsRelevant(s),
                    transform: TryGetProjectClass)
                .Where(static t => t is not null);

        var valueProvider = context.CompilationProvider.Combine(modulesAndTypes.Collect());

        context.RegisterSourceOutput(
            valueProvider,
            static (context, source) => Execute(context, source.Right!));
    }

    private static bool IsRelevant(SyntaxNode node)
    {
        if (node is ClassDeclarationSyntax { BaseList.Types.Count: > 0, TypeParameterList: null, })
        {
            return true;
        }

        if (node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax memberAccess,
            })
        {
            if (memberAccess.Name is GenericNameSyntax { TypeArgumentList.Arguments.Count: 1, } genericName)
            {
                if (genericName.Identifier.ValueText.Equals("AddFusionGateway", StringComparison.Ordinal))
                {
                    return true;
                }

                if (genericName.Identifier.ValueText.Equals("AddProject", StringComparison.Ordinal))
                {
                    var current = node;

                    while (current.Parent is InvocationExpressionSyntax or MemberAccessExpressionSyntax)
                    {
                        current = current.Parent;
                    }

                    if (current.Parent is EqualsValueClauseSyntax)
                    {
                        return true;
                    }
                }
            }

            if (memberAccess.Name is not GenericNameSyntax &&
                memberAccess.Name.Identifier.ValueText.Equals("WithSubgraph", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static ISyntaxInfo? TryGetProjectClass(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name: GenericNameSyntax
                    {
                        Identifier.ValueText: { } name,
                        TypeArgumentList: { Arguments.Count: 1, } args,
                    },
                },
            } invocation)
        {
            if (name.Equals("AddProject") &&
                context.SemanticModel.GetTypeInfo(args.Arguments[0]).Type is INamedTypeSymbol subgraphType &&
                subgraphType.AllInterfaces.Any(t => t.ToFullyQualified().Equals(WellKnownTypeNames.ProjectMetadata)))
            {
                SyntaxNode current = invocation;

                while (current.Parent is InvocationExpressionSyntax or MemberAccessExpressionSyntax)
                {
                    current = current.Parent;
                }

                if (current.Parent is EqualsValueClauseSyntax &&
                    current.Parent.Parent is VariableDeclaratorSyntax variable)
                {
                    return new SubgraphClass(
                        subgraphType.Name,
                        subgraphType.ToFullyQualified(),
                        variable.Identifier.ValueText);
                }
            }
        }

        if (context.Node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "WithSubgraph",
                },
                ArgumentList.Arguments.Count: 1,
            } subgraphInvocation)
        {
            SyntaxNode current = subgraphInvocation;

            while (current is InvocationExpressionSyntax or MemberAccessExpressionSyntax)
            {
                if (current is InvocationExpressionSyntax invocationSyntax)
                {
                    current = invocationSyntax.Expression;
                }
                else if (current is MemberAccessExpressionSyntax memberAccessSyntax)
                {
                    current = memberAccessSyntax.Expression;
                }

                if (current is InvocationExpressionSyntax
                    {
                        Expression: MemberAccessExpressionSyntax
                        {
                            Name: GenericNameSyntax
                            {
                                Identifier.ValueText: "AddFusionGateway",
                                TypeArgumentList.Arguments: { Count: 1, } fusionArgs,
                            },
                        },
                    } &&
                    context.SemanticModel.GetTypeInfo(fusionArgs[0]).Type is INamedTypeSymbol gatewayType)
                {
                    var argument = subgraphInvocation.ArgumentList.Arguments[0];
                    return new GatewayClass(
                        gatewayType.Name,
                        gatewayType.ToFullyQualified(),
                        GetVariableName(argument));
                }
            }
        }


        return null;
    }

    private static string GetVariableName(ArgumentSyntax argument)
    {
        SyntaxNode current = argument.Expression;

        while (current is InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax)
        {
            if (current is InvocationExpressionSyntax invocation)
            {
                current = invocation.Expression;
            }

            if (current is MemberAccessExpressionSyntax memberAccess)
            {
                current = memberAccess.Expression;
            }

            if (current is IdentifierNameSyntax identifier)
            {
                return identifier.Identifier.ValueText;
            }
        }

        throw new InvalidOperationException();
    }

    private static void Execute(
        SourceProductionContext context,
        ImmutableArray<ISyntaxInfo> syntaxInfos)
    {
        if (syntaxInfos.Length == 0)
        {
            WriteNoOpCompose(context);
            return;
        }

        var projects = new Dictionary<string, SubgraphClass>();

        foreach (var project in syntaxInfos.OfType<SubgraphClass>())
        {
            projects[project.VariableName] = project;
        }

        var processed = new HashSet<string>();
        var gateways = new List<GatewayInfo>();

        foreach (var gatewayGroup in syntaxInfos.OfType<GatewayClass>().GroupBy(t => t.Name))
        {
            if (!processed.Add(gatewayGroup.Key))
            {
                continue;
            }

            var gateway = new GatewayInfo(gatewayGroup.Key, gatewayGroup.First().TypeName);

            foreach (var projectLink in gatewayGroup)
            {
                if (projects.TryGetValue(projectLink.VariableName, out var project))
                {
                    gateway.Subgraphs.Add(new SubgraphInfo(project.Name, project.TypeName));
                }
            }
            
            gateways.Add(gateway);
        }

        if (gateways.Count == 0)
        {
            WriteNoOpCompose(context);
            return;
        }
        
        WriteCompose(context);

        var code = StringBuilderPool.Get();
        using var writer = new CodeWriter(code);

        writer.WriteFileHeader();
        writer.Write(AnalyzerResources.CliCode);
        writer.WriteLine();
        writer.WriteLine();
        writer.WriteIndentedLine("namespace HotChocolate.Fusion.Composition.Tooling");
        writer.WriteIndentedLine("{");

        using (writer.IncreaseIndent())
        {
            writer.WriteIndentedLine("file class GatewayList : List<GatewayInfo>");
            writer.WriteIndentedLine("{");

            using (writer.IncreaseIndent())
            {
                writer.WriteIndentedLine("public GatewayList()");

                using (writer.IncreaseIndent())
                {
                    writer.WriteIndentedLine(": base(");
                    writer.WriteIndentedLine("[");

                    using (writer.IncreaseIndent())
                    {
                        foreach (var gateway in gateways)
                        {
                            writer.WriteIndentedLine("GatewayInfo.Create<{0}>(", gateway.TypeName);

                            using (writer.IncreaseIndent())
                            {
                                writer.WriteIndentedLine("\"{0}\",", gateway.Name);

                                var first = true;

                                foreach (var project in gateway.Subgraphs)
                                {
                                    if (first)
                                    {
                                        first = false;
                                    }
                                    else
                                    {
                                        writer.Write(",");
                                        writer.WriteLine();
                                    }

                                    writer.WriteIndented(
                                        "SubgraphInfo.Create<{0}>(\"{1}\", \"{2}\")",
                                        project.TypeName,
                                        project.Name,
                                        project.Name);
                                }

                                writer.Write("),");
                                writer.WriteLine();
                            }
                        }
                    }

                    writer.WriteIndentedLine("]) { }");
                }
            }

            writer.WriteIndentedLine("}");
        }

        writer.WriteIndentedLine("}");

        context.AddSource("FusionConfiguration.g.cs", code.ToString());
        StringBuilderPool.Return(code);
    }
    
    private static void WriteCompose(SourceProductionContext context)
    {
        var code = StringBuilderPool.Get();
        using var writer = new CodeWriter(code);

        writer.WriteFileHeader();
        writer.Write(AnalyzerResources.Compose);

        context.AddSource("FusionCompose.g.cs", code.ToString());
        StringBuilderPool.Return(code);
    }

    private static void WriteNoOpCompose(SourceProductionContext context)
    {
        var code = StringBuilderPool.Get();
        using var writer = new CodeWriter(code);

        writer.WriteFileHeader();
        writer.Write(AnalyzerResources.NoOpCompose);

        context.AddSource("FusionCompose.g.cs", code.ToString());
        StringBuilderPool.Return(code);
    }

    private static void AddInitializationContent(IncrementalGeneratorPostInitializationContext context)
    {
        var code = StringBuilderPool.Get();
        using var writer = new CodeWriter(code);

        writer.WriteFileHeader();
        writer.Write(AnalyzerResources.Extensions);

        context.AddSource("FusionExtensions.g.cs", code.ToString());
        StringBuilderPool.Return(code);
    }
}