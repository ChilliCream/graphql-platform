using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using HotChocolate.CodeGeneration.Neo4J.Types;
using HotChocolate.CodeGeneration.Types;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using static HotChocolate.CodeGeneration.TypeNames;
using static HotChocolate.CodeGeneration.Neo4J.Neo4JTypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace HotChocolate.CodeGeneration.Neo4J
{
    public partial class Neo4JCodeGenerator : ICodeGenerator
    {
        public CodeGenerationResult Generate(CodeGeneratorContext context)
        {
            var result = new CodeGenerationResult();

            var schema = SchemaHelper.CreateSchema(context.Documents);

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
            CodeGeneratorContext generatorContext,
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
            CodeGeneratorContext generatorContext,
            IReadOnlyList<IObjectType> objectTypes)
        {
            var queryDeclaration =
                ClassDeclaration("Query") // todo : we need to read the name from the config
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute()
                    .AddExtendObjectTypeAttribute("Query");

            foreach (var objectType in objectTypes)
            {
                queryDeclaration = queryDeclaration.AddMembers(
                    CreateQueryResolver(dataContext, generatorContext, objectType));

                GenerateObjectType(result, generatorContext.Namespace, objectType);
            }

            var namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(generatorContext.Namespace))
                    .AddMembers(queryDeclaration);

            var compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            result.AddSource(
                generatorContext.Namespace + ".Query.cs",
                compilationUnit.ToFullString());
        }

        private static void GenerateObjectType(
            CodeGenerationResult result,
            string @namespace,
            IObjectType objectType)
        {
            var typeNameDirective = objectType.GetFirstDirective<TypeNameDirective>("typeName");
            var typeName = typeNameDirective?.Name ?? objectType.Name;

            var modelDeclaration =
                ClassDeclaration(typeName)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute();

            foreach (var field in objectType.Fields.Where(t => !t.IsIntrospectionField))
            {
                var relationship =
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

            var namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(@namespace))
                    .AddMembers(modelDeclaration);

            var compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            result.AddSource(@namespace + $".{typeName}.cs", compilationUnit.ToFullString());
        }

        private static MethodDeclarationSyntax CreateQueryResolver(
            DataGeneratorContext dataContext,
            CodeGeneratorContext generatorContext,
            IObjectType objectType)
        {
            const string session = nameof(session);

            dataContext = DataGeneratorContext.FromMember(objectType, dataContext);

            var typeNameDirective =
                objectType.GetFirstDirective<TypeNameDirective>("typeName");
            var typeName = typeNameDirective?.Name ?? objectType.Name;
            var pluralTypeName = typeNameDirective?.PluralName ?? typeName + "s";

            var resolverSyntax =
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
                .AddNeo4JDatabaseAttribute(generatorContext.DatabaseName)
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
            CodeGeneratorContext generatorContext)
        {
            var typeName = generatorContext.Name + "RequestExecutorBuilderExtensions";

            var dependencyInjectionCode =
                ClassDeclaration(typeName)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute();

            var statements = new List<StatementSyntax>
            {
                AddTypeExtension(Global(generatorContext.Namespace + ".Query")),
                AddNeo4JFiltering(),
                AddNeo4JSorting(),
                AddNeo4JProjections(),
                ReturnStatement(IdentifierName("builder"))
            };

            var addTypes =
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

            var namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(DependencyInjection))
                    .AddMembers(dependencyInjectionCode);

            var compilationUnit =
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
    }
}
