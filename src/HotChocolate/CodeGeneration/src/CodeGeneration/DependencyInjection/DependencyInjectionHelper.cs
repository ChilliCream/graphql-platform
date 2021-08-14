using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.CodeGeneration.TypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace HotChocolate.CodeGeneration.DependencyInjection
{
    public static class DependencyInjectionHelper
    {
        public static ExpressionStatementSyntax AddTypeExtension(
            string typeExtensions, string builderName = "builder")
        {
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(Global(SchemaRequestExecutorBuilderExtensions)),
                        GenericName(Identifier("AddTypeExtension"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(typeExtensions))))))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList<ArgumentSyntax>(
                            Argument(IdentifierName(builderName))))));
        }

        public static void GenerateDependencyInjectionCode(
            CodeGenerationResult result,
            CodeGeneratorContext generatorContext,
            List<StatementSyntax> additionalStatements)
        {
            var typeName = generatorContext.Name + "RequestExecutorBuilderExtensions";

            ClassDeclarationSyntax dependencyInjectionCode =
                ClassDeclaration(typeName)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute();

            var statements = new List<StatementSyntax>
            {
                AddTypeExtension(Global(generatorContext.Namespace + ".Query"))
            };

            statements.AddRange(additionalStatements);

            statements.Add(ReturnStatement(IdentifierName("builder")));

            MethodDeclarationSyntax addTypes =
                MethodDeclaration(
                    IdentifierName(Global(IRequestExecutorBuilder)),
                    Identifier("Add" + generatorContext.Name + "Types"))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier("builder"))
                                .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
                                .WithType(IdentifierName(Global(IRequestExecutorBuilder))))))
                .WithBody(Block(statements));

            dependencyInjectionCode =
                dependencyInjectionCode.AddMembers(addTypes);

            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(MsExtDependencyInjection))
                    .AddMembers(dependencyInjectionCode);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            result.AddClass(MsExtDependencyInjection, typeName, compilationUnit.ToFullString());
        }
    }
}
