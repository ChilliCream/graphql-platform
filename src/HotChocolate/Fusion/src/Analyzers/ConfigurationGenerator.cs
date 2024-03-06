using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Fusion.Analyzers.Properties;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class ConfigurationGenerator : IIncrementalGenerator
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
            if (memberAccess.Name is GenericNameSyntax genericName &&
                genericName.TypeArgumentList.Arguments.Count == 1)
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
                    return new ProjectClass(
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
                    } parentInvocation &&
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
        Compilation compilation,
        ImmutableArray<ISyntaxInfo> syntaxInfos)
    {
        if (syntaxInfos.Length == 0)
        {
            return;
        }

        var projects = new Dictionary<string, ProjectClass>();
        foreach (var project in syntaxInfos.OfType<ProjectClass>())
        {
            projects[project.VariableName] = project;
        }

        var gateways = new List<GatewayInfo>();
        foreach (var gatewayGroup in syntaxInfos.OfType<GatewayClass>().GroupBy(t => t.Name))
        {
            var gateway = new GatewayInfo(gatewayGroup.Key, gatewayGroup.First().TypeName);

            foreach (var projectLink in gatewayGroup)
            {
                if (projects.TryGetValue(projectLink.VariableName, out var project))
                {
                    gateway.Projects.Add(new ProjectInfo(project.Name, project.TypeName));
                    gateways.Add(gateway);
                }
            }
            
        }

        if (gateways.Count == 0)
        {
            return;
        }

        var code = StringBuilderPool.Get();
        using var writer = new CodeWriter(code);
        writer.WriteFileHeader();
        writer.WriteLine();
        writer.Write(Resources.CliCode);
        writer.WriteLine();
        writer.WriteLine();
        writer.WriteIndentedLine("file static class FusionGatewayConfigurationFiles");
        writer.WriteIndentedLine("{");

        using (writer.IncreaseIndent())
        {
            writer.WriteIndentedLine("public static readonly string[] SubgraphProjects =");
            writer.WriteIndentedLine("[");

            using (writer.IncreaseIndent())
            {
                foreach (var project in gateways[0].Projects)
                {
                    writer.WriteIndentedLine("new {0}().ProjectPath,", project.TypeName);
                }
            }

            writer.WriteIndentedLine("];");
            writer.WriteLine();
            writer.WriteIndentedLine("public static string GatewayProject");

            using (writer.IncreaseIndent())
            {
                writer.WriteIndentedLine("=> new {0}().ProjectPath;", gateways[0].TypeName);
            }
        }


        writer.WriteIndentedLine("}");

        context.AddSource("FusionGatewayConfiguration.g.cs", code.ToString());
        StringBuilderPool.Return(code);
    }
}