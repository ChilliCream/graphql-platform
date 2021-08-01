using System.Collections.Generic;
using System.Linq;
using HotChocolate.CodeGeneration.Types;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.CodeGeneration.TypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace HotChocolate.CodeGeneration
{
    public partial class Neo4JCodeGenerator : INeo4JCodeGenerator
    {
        public CodeGenerationResult Generate(Neo4JCodeGeneratorContext context)
        {
            var result = new CodeGenerationResult();

            ISchema schema = SchemaHelper.CreateSchema(context.Documents);

            GenerateTypes(
                result,
                DataGeneratorContext.FromSchema(schema),
                context,
                schema);

            return result;
        }

        private static void GenerateTypes(
            CodeGenerationResult result,
            DataGeneratorContext dataContext,
            Neo4JCodeGeneratorContext generatorContext,
            ISchema schema)
        {
            GenerateQueryType(
                result,
                dataContext,
                generatorContext,
                schema.Types
                    .OfType<ObjectType>()
                    .Where(type => !IntrospectionTypes.IsIntrospectionType(type))
                    .ToList());

            GenerateDependencyInjectionCode(
                result,
                generatorContext);
        }

        private static void GenerateQueryType(
            CodeGenerationResult result,
            DataGeneratorContext dataContext,
            Neo4JCodeGeneratorContext generatorContext,
            IReadOnlyList<IObjectType> objectTypes)
        {
            ClassDeclarationSyntax queryDeclaration =
                ClassDeclaration("Query") // todo : we need to read the name from the config
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute()
                    .AddExtendObjectTypeAttribute("Query");

            foreach (IObjectType? objectType in objectTypes)
            {
                queryDeclaration = queryDeclaration.AddMembers(
                    CreateQueryResolver(dataContext, generatorContext, objectType));

                GenerateObjectType(result, generatorContext.Namespace!, objectType);
            }

            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(generatorContext.Namespace!))
                    .AddMembers(queryDeclaration);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            result.AddSource(generatorContext.Namespace! + ".Query.cs", compilationUnit.ToFullString());
        }

        private static void GenerateObjectType(
            CodeGenerationResult result,
            string @namespace,
            IObjectType objectType)
        {
            TypeNameDirective? typeNameDirective =
                objectType.GetFirstDirective<TypeNameDirective>("typeName");
            var typeName = typeNameDirective?.Name ?? objectType.Name.Value;

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

            result.AddSource(@namespace + $".{typeName}.cs", compilationUnit.ToFullString());
        }

        private static MethodDeclarationSyntax CreateQueryResolver(
            DataGeneratorContext dataContext,
            Neo4JCodeGeneratorContext context,
            IObjectType objectType)
        {
            const string session = nameof(session);

            dataContext = DataGeneratorContext.FromMember(objectType, dataContext);

            TypeNameDirective? typeNameDirective =
                objectType.GetFirstDirective<TypeNameDirective>("typeName");
            var typeName = typeNameDirective?.Name ?? objectType.Name.Value;
            var pluralTypeName = typeNameDirective?.PluralName ?? typeName + "s";

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
                .AddNeo4JDatabaseAttribute(context.DatabaseName)
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
            CodeGenerationResult result,
            Neo4JCodeGeneratorContext codeGeneratorContext)
        {
            var typeName = codeGeneratorContext.Name + "RequestExecutorBuilderExtensions";

            ClassDeclarationSyntax dependencyInjectionCode =
                ClassDeclaration(typeName)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute();

            var statements = new List<StatementSyntax>
            {
                AddTypeExtension(Global(codeGeneratorContext.Namespace + ".Query")),
                AddNeo4JFiltering(),
                AddNeo4JSorting(),
                AddNeo4JProjections(),
                ReturnStatement(IdentifierName("builder"))
            };

            MethodDeclarationSyntax addTypes =
                MethodDeclaration(
                    IdentifierName(Global(IRequestExecutorBuilder)),
                    Identifier("Add" + codeGeneratorContext.Name + "Types"))
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

            result.AddSource(DependencyInjection + $".{typeName}.cs", compilationUnit.ToFullString());
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
                        SingletonSeparatedList<ArgumentSyntax>(
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
                        SingletonSeparatedList<ArgumentSyntax>(
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
                        SingletonSeparatedList<ArgumentSyntax>(
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
                        SingletonSeparatedList<ArgumentSyntax>(
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
    }
}
