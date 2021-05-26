using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HotChocolate.Data.Neo4J.Analyzers.Types;
using HotChocolate.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.Data.Neo4J.Analyzers.TypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using IOPath = System.IO.Path;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;


namespace HotChocolate.Data.Neo4J.Analyzers
{
    [Generator]
    public class DataSourceGenerator : ISourceGenerator
    {
        private static string _location = IOPath.GetDirectoryName(
            typeof(DataSourceGenerator).Assembly.Location)!;

        static DataSourceGenerator()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        private static Assembly? CurrentDomainOnAssemblyResolve(
            object sender,
            ResolveEventArgs args)
        {
            try
            {
                var assemblyName = new AssemblyName(args.Name);
                var path = IOPath.Combine(_location, assemblyName.Name + ".dll");
                return Assembly.LoadFrom(path);
            }
            catch
            {
                return null;
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                IReadOnlyList<string> files = Files.GetGraphQLFiles(context);

                if (files.Count > 0)
                {
                    ISchema schema = SchemaHelper.CreateSchema(files);
                }
            }
            catch(Exception ex)
            {
                context.AddSource("error.cs", "/*" + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + "*/");
            }
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

            foreach (var objectType in objectTypes)
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
