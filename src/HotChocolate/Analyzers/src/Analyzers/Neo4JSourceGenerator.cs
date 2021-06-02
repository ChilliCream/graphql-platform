using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Analyzers.Configuration;
using HotChocolate.Analyzers.Diagnostics;
using HotChocolate.Analyzers.Types;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.Analyzers.TypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace HotChocolate.Analyzers
{
    [Generator]
    public partial class Neo4JSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {
            _location = context.GetBinDirectory();

            try
            {
                foreach (GraphQLConfig config in context.GetConfigurations())
                {

                    if (config.Extensions.Neo4J is not null)
                    {
                        context.AddSource("abc.cs", "// abc");
                        var schemaDocuments = context.GetSchemaDocuments(config);
                        context.AddSource("def.cs", "// abc");

                        if (schemaDocuments.Count > 0)
                        {
                            context.AddSource("def1.cs", "// abc");
                            config.Extensions.Neo4J.Namespace ??= "GraphQL";
                            ISchema schema = SchemaHelper.CreateSchema(schemaDocuments);
                            DataGeneratorContext dataContext = DataGeneratorContext.FromSchema(schema);
                            GenerateTypes(context, dataContext, config.Extensions.Neo4J, schema);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                context.ReportError(ex);
            }
        }

        private static void GenerateTypes(
            GeneratorExecutionContext context,
            DataGeneratorContext dataContext,
            Neo4JSettings settings,
            ISchema schema)
        {
            GenerateQueryType(
                context,
                dataContext,
                settings,
                schema.Types
                    .OfType<ObjectType>()
                    .Where(type => !IntrospectionTypes.IsIntrospectionType(type))
                    .ToList());
        }

        private static void GenerateQueryType(
            GeneratorExecutionContext context,
            DataGeneratorContext dataContext,
            Neo4JSettings settings,
            IReadOnlyList<IObjectType> objectTypes)
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
                queryDeclaration = queryDeclaration.AddMembers(
                    CreateQueryResolver(dataContext, objectType));

                GenerateObjectType(context, dataContext, settings.Namespace!, objectType);
            }

            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(settings.Namespace!))
                    .AddMembers(queryDeclaration);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            context.AddSource(settings.Namespace! + ".Query.cs", compilationUnit.ToFullString());
        }

        private static void GenerateObjectType(
            GeneratorExecutionContext context,
            DataGeneratorContext dataContext,
            string @namespace,
            IObjectType objectType)
        {
            var typeNameDirective = objectType.GetFirstDirective<TypeNameDirective>("typeName");
            string typeName = typeNameDirective?.Name ?? objectType.Name.Value;

            ClassDeclarationSyntax modelDeclaration =
                ClassDeclaration(typeName)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute();

            foreach (IObjectField field in objectType.Fields.Where(t => !t.IsIntrospectionField))
            {
                modelDeclaration =
                    modelDeclaration.AddProperty(
                        field.GetPropertyName(),
                        IdentifierName(field.GetTypeName(@namespace)),
                        field.Description);
            }

            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(@namespace))
                    .AddMembers(modelDeclaration);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            context.AddSource(@namespace + $".{typeName}.cs", compilationUnit.ToFullString());
        }

        private static MethodDeclarationSyntax CreateQueryResolver(
            DataGeneratorContext dataContext,
            IObjectType objectType)
        {
            const string session = nameof(session);

            dataContext = DataGeneratorContext.FromMember(objectType, dataContext);

            var typeNameDirective = objectType.GetFirstDirective<TypeNameDirective>("typeName");
            string typeName = typeNameDirective?.Name ?? objectType.Name.Value;
            string pluralTypeName = typeNameDirective?.PluralName ?? typeName + "s";

            MethodDeclarationSyntax resolverSyntax =
                MethodDeclaration(
                    GenericName(Identifier(Global(Neo4JExecutable)))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(typeName)))),
                    Identifier("Get" + pluralTypeName))
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
                                    Argument(IdentifierName("session")))))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .AddPagingAttribute(dataContext.Paging);

            if (dataContext.Filtering)
            {
                resolverSyntax = resolverSyntax.AddFilteringAttribute();
            }

            if (dataContext.Sorting)
            {
                resolverSyntax = resolverSyntax.AddSortingAttribute();
            }

            return resolverSyntax;
        }
    }
}
