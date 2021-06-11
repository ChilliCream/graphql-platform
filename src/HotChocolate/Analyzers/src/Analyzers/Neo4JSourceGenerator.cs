using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotChocolate.Analyzers.Configuration;
using HotChocolate.Analyzers.Diagnostics;
using HotChocolate.Analyzers.Types;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.IO.Path;
using static HotChocolate.Analyzers.TypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace HotChocolate.Analyzers
{
    [Generator]
    public partial class Neo4JSourceGenerator : ISourceGenerator
    {
        private const string _allFiles = "*.*";

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
                        context.GetSchemaDocuments(config) is {Count: > 0} schemaDocuments)
                    {
                        ISchema schema = SchemaHelper.CreateSchema(schemaDocuments);
                        string generated = Combine(GetDirectoryName(config.Location)!, "Generated");
                        var dataContext = DataGeneratorContext.FromSchema(schema);
                        Neo4JSettings settings = config.Extensions.Neo4J;
                        settings.Generated ??= generated;

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
            if (settings.EmitCode && !Directory.Exists(settings.Generated))
            {
                Directory.CreateDirectory(settings.Generated);
            }

            if (Directory.Exists(settings.Generated))
            {
                foreach (string fileName in Directory.GetFiles(
                    settings.Generated,
                    _allFiles,
                    SearchOption.AllDirectories))
                {
                    File.Delete(fileName);
                }
            }

            GenerateQueryType(
                context,
                dataContext,
                settings,
                schema.Types
                    .OfType<ObjectType>()
                    .Where(type => !IntrospectionTypes.IsIntrospectionType(type))
                    .ToList());

            GenerateDependencyInjectionCode(context, settings);
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

                GenerateObjectType(context, settings, settings.Namespace!, objectType);
            }

            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(settings.Namespace!))
                    .AddMembers(queryDeclaration);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            AddSource(context, compilationUnit, settings, settings.Namespace!, "Query.hc.g.cs");
        }

        private static void GenerateObjectType(
            GeneratorExecutionContext context,
            Neo4JSettings settings,
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
                RelationshipDirective? relationship =
                    field.GetFirstDirective<RelationshipDirective>("relationship");

                modelDeclaration =
                    modelDeclaration.AddProperty(
                        field.GetPropertyName(),
                        IdentifierName(field.GetTypeName(@namespace)),
                        field.Description,
                        setable: true,
                        configure: p =>
                        {
                            if (relationship is not null)
                            {
                                p = p.AddNeo4JRelationshipAttribute(
                                    relationship.Name,
                                    relationship.Direction);
                            }

                            return p;
                        });
            }

            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(@namespace))
                    .AddMembers(modelDeclaration);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            AddSource(context, compilationUnit, settings, @namespace, $"{typeName}.hc.g.cs");
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
                    .AddNeo4JDatabaseAttribute(settings.DatabaseName)
                    .AddPagingAttribute(dataContext.Paging)
                    .AddProjectionAttribute();

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

        private static void GenerateDependencyInjectionCode(
            GeneratorExecutionContext context,
            Neo4JSettings settings)
        {
            string typeName = settings.Name + "RequestExecutorBuilderExtensions";

            ClassDeclarationSyntax dependencyInjectionCode =
                ClassDeclaration(typeName)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute();

            var statements = new List<StatementSyntax>
            {
                AddTypeExtension(Global(settings.Namespace + ".Query")),
                AddNeo4JFiltering(),
                AddNeo4JSorting(),
                AddNeo4JProjections(),
                ReturnStatement(IdentifierName("builder"))
            };

            MethodDeclarationSyntax addTypes =
                MethodDeclaration(
                        IdentifierName(Global(IRequestExecutorBuilder)),
                        Identifier("Add" + settings.Name + "Types"))
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
                NamespaceDeclaration(IdentifierName(DependencyInjection))
                    .AddMembers(dependencyInjectionCode);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            AddSource(
                context,
                compilationUnit,
                settings,
                DependencyInjection,
                $"{typeName}.hc.g.cs");
        }

        private static ExpressionStatementSyntax AddTypeExtension(string typeExtensions)
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
                            SingletonSeparatedList(
                                Argument(IdentifierName("builder"))))));
        }

        private static ExpressionStatementSyntax AddNeo4JFiltering()
        {
            return ExpressionStatement(
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(Global(Neo4JDataRequestBuilderExtensions)),
                            IdentifierName("AddNeo4JFiltering")))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(IdentifierName("builder"))))));
        }

        private static ExpressionStatementSyntax AddNeo4JSorting()
        {
            return ExpressionStatement(
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(Global(Neo4JDataRequestBuilderExtensions)),
                            IdentifierName("AddNeo4JSorting")))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(IdentifierName("builder"))))));
        }

        private static ExpressionStatementSyntax AddNeo4JProjections()
        {
            return ExpressionStatement(
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(Global(Neo4JDataRequestBuilderExtensions)),
                            IdentifierName("AddNeo4JProjections")))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(IdentifierName("builder"))))));
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

        private static void AddSource(
            GeneratorExecutionContext context,
            CompilationUnitSyntax compilationUnit,
            Neo4JSettings settings,
            string @namespace,
            string fileName)
        {
            fileName = @namespace + "." + fileName;
            string sourceText = compilationUnit.ToFullString();
            context.AddSource(fileName, sourceText);

            if (settings.EmitCode)
            {
                File.WriteAllText(Combine(settings.Generated, fileName), sourceText);
            }
        }
    }
}
