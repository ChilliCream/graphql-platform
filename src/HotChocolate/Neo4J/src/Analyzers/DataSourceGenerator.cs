using System.Linq;
using System.Collections.Generic;
using System.IO;
using HotChocolate.Data.Neo4J.Analyzers.Types;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.CodeAnalysis;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static HotChocolate.Data.Neo4J.Analyzers.TypeNames;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;
using IHasDirectives = HotChocolate.Language.IHasDirectives;

namespace HotChocolate.Data.Neo4J.Analyzers
{
    public class DataSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("Foo.cs", "bar");
        }

        private static void GenerateTypes(
            GeneratorExecutionContext context,
            IReadOnlyList<ObjectTypeDefinitionNode> typeDefinitions)
        {
            const string @namespace = "Foo.Bar";

            GenerateQueryType(context, @namespace, typeDefinitions);
        }

        private static void GenerateQueryType(
            GeneratorExecutionContext context,
            string @namespace,
            IReadOnlyList<ObjectTypeDefinitionNode> objectTypes)
        {
            ClassDeclarationSyntax queryDeclaration =
                ClassDeclaration("Query") // todo : we need to read the name from the config
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute()
                    .AddExtendObjectTypeAttribute("Query");

            foreach (ObjectTypeDefinitionNode objectType in objectTypes)
            {
                string typeName = objectType.Name.Value;
                string pluralTypeName = typeName + "s"; // TODO : plural directive

                queryDeclaration = queryDeclaration.AddMembers(
                    CreateQueryResolver(typeName, pluralTypeName));
            }

            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(@namespace))
                    .AddMembers(queryDeclaration);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            context.AddSource(@namespace + ".Query.cs", compilationUnit.ToFullString());
        }

        private static void GenerateObjectType(
            GeneratorExecutionContext context,
            ObjectTypeDefinitionNode objectTypeDefinition)
        {

        }

        private static MethodDeclarationSyntax CreateQueryResolver(
            string typeName,
            string pluralTypeName)
        {
            const string session = nameof(session);

            return MethodDeclaration(
                    GenericName(Identifier(Neo4JExecutable))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(typeName)))),
                    Identifier(pluralTypeName))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier(session))
                                .WithAttributeLists(
                                    SingletonList(
                                        AttributeList(
                                            SingletonSeparatedList(
                                                Attribute(IdentifierName("ScopedService"))))))
                                .WithType(IdentifierName("IAsyncSession")))))
                .WithExpressionBody(
                    ArrowExpressionClause(
                        ImplicitObjectCreationExpression()
                            .WithArgumentList(
                                ArgumentList(SingletonSeparatedList(
                                    Argument(IdentifierName("session")))))));
        }
    }
}
