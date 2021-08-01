using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.CodeGeneration
{
    public class CodeGenerationResult
    {
        public IList<SourceFile> SourceFiles { get; } = new List<SourceFile>();

        public void AddSource(string fileName, string source)
        {
            SourceFiles.Add(new SourceFile(fileName, source));
        }

        public void AddClass(string @namespace, string @className, string source)
        {
            AddSource($"{@namespace}.{className}.cs", source);
        }

        public void AddClass(string @namespace, string className, ClassDeclarationSyntax classDeclarationSyntax)
        {
            AddSource($"{@namespace}.{className}.cs", classDeclarationSyntax.ToFullString());
        }

        public void AddClass(
            string @namespace,
            string className,
            ClassDeclarationSyntax classDeclaration,
            UsingDirectiveSyntax[] usings)
        {
            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(@namespace))
                    .AddMembers(classDeclaration);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration)
                    .AddUsings(usings)
                    .NormalizeWhitespace(elasticTrivia: true);

            context.AddSource(@namespace + $".{className}.cs", compilationUnit.ToFullString());
        }
    }
}
