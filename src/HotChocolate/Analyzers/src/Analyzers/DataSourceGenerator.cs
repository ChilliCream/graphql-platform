using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Data.Neo4J.Analyzers.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.Data.Neo4J.Analyzers.TypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;


namespace HotChocolate.Data.Neo4J.Analyzers
{
    [Generator]
    public partial class DataSourceGenerator : ISourceGenerator
    {
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
                    DataGeneratorContext dataContext = DataGeneratorContext.FromSchema(schema);
                    GenerateTypes(context, dataContext, schema);
                }
            }
            catch (Exception ex)
            {
                context.AddSource(
                    "error.cs",
                    "/*" + Environment.NewLine +
                    ex.Message + Environment.NewLine +
                    ex.StackTrace + Environment.NewLine + "*/");
            }
        }

        private static void GenerateTypes(
            GeneratorExecutionContext context,
            DataGeneratorContext dataContext,
            ISchema schema)
        {
            const string @namespace = "Foo.Bar";

            GenerateQueryType(
                context,
                dataContext,
                @namespace,
                schema.Types
                    .OfType<ObjectType>()
                    .Where(type => !IntrospectionTypes.IsIntrospectionType(type))
                    .ToList());
        }

        private static void GenerateQueryType(
            GeneratorExecutionContext context,
            DataGeneratorContext dataContext,
            string @namespace,
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

                GenerateObjectType(context, dataContext, @namespace, objectType);
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

    public static class SchemaExtensions
    {
        public static T? GetFirstDirective<T>(
            this HotChocolate.Types.IHasDirectives hasDirectives,
            string name,
            T? defaultValue = default)
        {
            var directive = hasDirectives.Directives[name].FirstOrDefault();

            if (directive is null)
            {
                return defaultValue;
            }

            return directive.ToObject<T>();
        }

        public static string GetPropertyName(this IObjectField field)
        {
            if (field.Name.Value.Length == 1)
            {
                return field.Name.Value.ToUpperInvariant();
            }

            return field.Name.Value.Substring(0, 1).ToUpperInvariant() +
                field.Name.Value.Substring(1);
        }

        public static string GetTypeName(this IObjectField field, string @namespace)
        {
            return CreateTypeName(field.Type, @namespace);
        }

        public static string CreateTypeName(IType type, string @namespace, bool nullable = true)
        {
            if (type.IsNonNullType())
            {
                return CreateTypeName(type.InnerType(), @namespace, false);
            }

            if (type.IsListType())
            {
                string elementType = CreateTypeName(type.ElementType(), @namespace, true);
                string listType = Generics(Global(TypeNames.List), elementType);

                if (nullable)
                {
                    return Nullable(listType);
                }

                return listType;
            }

            if (type.IsScalarType())
            {
                Type runtimeType = type.ToRuntimeType();

                if (runtimeType.IsGenericType &&
                    runtimeType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    runtimeType = runtimeType.GetGenericArguments()[0];
                }

                return Global(ToTypeName(runtimeType));
            }

            if (type is ObjectType objectType)
            {
                var typeNameDirective = objectType.GetFirstDirective<TypeNameDirective>("typeName");
                string typeName = typeNameDirective?.Name ?? objectType.Name.Value;
                return Global(typeName);
            }

            throw new NotSupportedException();
        }

        private static string ToTypeName(this Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.IsGenericType
                ? CreateGenericTypeName(type)
                : CreateTypeName(type, type.Name);
        }

        private static string CreateGenericTypeName(Type type)
        {
            string name = type.Name.Substring(0, type.Name.Length - 2);
            IEnumerable<string> arguments = type.GetGenericArguments().Select(ToTypeName);
            return CreateTypeName(type, $"{name}<{string.Join(", ", arguments)}>");
        }

        private static string CreateTypeName(Type type, string typeName)
        {
            string ns = GetNamespace(type);
            if (ns is null)
            {
                return typeName;
            }
            return $"{ns}.{typeName}";
        }

        private static string GetNamespace(Type type)
        {
            if (type.IsNested)
            {
                return $"{GetNamespace(type.DeclaringType)}.{type.DeclaringType.Name}";
            }
            return type.Namespace;
        }
    }
}
