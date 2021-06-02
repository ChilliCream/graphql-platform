using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Analyzers.Configuration;
using HotChocolate.Analyzers.Diagnostics;
using HotChocolate.Analyzers.Types;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using Microsoft.CodeAnalysis;
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
            ExecuteInternal(context);
        }

        private void ExecuteInternal(GeneratorExecutionContext context)
        {
            try
            {
                foreach (GraphQLConfig config in context.GetConfigurations())
                {

                    if (config.Extensions.Neo4J is not null &&
                        context.GetSchemaDocuments(config) is { Count: > 0 } schemaDocuments)
                    {
                        ISchema schema = SchemaHelper.CreateSchema(schemaDocuments);
                        DataGeneratorContext dataContext = DataGeneratorContext.FromSchema(schema);
                        GenerateTypes(context, dataContext, config.Extensions.Neo4J, schema);
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
                    CreateQueryResolver(dataContext, settings, objectType));

                GenerateObjectType(context, settings.Namespace!, objectType);
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
            string @namespace,
            IObjectType objectType)
        {
            TypeNameDirective? typeNameDirective =
                objectType.GetFirstDirective<TypeNameDirective>("typeName");
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
            Neo4JSettings settings,
            IObjectType objectType)
        {
            const string session = nameof(session);

            dataContext = DataGeneratorContext.FromMember(objectType, dataContext);

            TypeNameDirective? typeNameDirective =
                objectType.GetFirstDirective<TypeNameDirective>("typeName");
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
                                .AddScopedServiceAttribute()
                                .WithType(IdentifierName(Global(IAsyncSession))))))
                .WithExpressionBody(
                    ArrowExpressionClause(
                        ImplicitObjectCreationExpression()
                            .WithArgumentList(
                                ArgumentList(SingletonSeparatedList(
                                    Argument(IdentifierName("session")))))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .AddGraphQLNameAttribute(GraphQLFieldName(pluralTypeName))
                .AddUseNeo4JDatabaseAttribute(settings.DatabaseName)
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

        private static string GraphQLFieldName(string s)
        {
            var buffer = new char[s.Length];
            var lower = true;

            for (var i = 0; i < s.Length; i++)
            {
                if (lower && char.IsUpper(s[i]))
                {
                    buffer[i] = char.ToLowerInvariant(s[i]);
                }
                else
                {
                    lower = false;
                    buffer[i] = s[i];
                }
            }

            return new string(buffer);
        }
    }
}
