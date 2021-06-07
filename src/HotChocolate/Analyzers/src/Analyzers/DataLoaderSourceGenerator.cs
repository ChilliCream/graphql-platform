using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Analyzers
{
    public class DataLoaderSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new DataLoaderCollector());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var collector = (DataLoaderCollector)context.SyntaxReceiver!;
        }

        public class DataLoaderCollector : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> PartialDataLoaderClasses { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax classDeclaration &&
                    classDeclaration.ChildTokens().Any(t => t.Kind() is SyntaxKind.PartialKeyword) &&
                    classDeclaration.Identifier.ToString().EndsWith("DataLoader"))
                {
                    PartialDataLoaderClasses.Add(classDeclaration);
                }
            }
        }
    }
}
