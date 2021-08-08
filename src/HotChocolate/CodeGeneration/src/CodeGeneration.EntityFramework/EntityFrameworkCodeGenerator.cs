using System;
using System.Linq;
using HotChocolate.CodeGeneration.EntityFramework.Types;
using HotChocolate.CodeGeneration.Types;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.CodeGeneration.TypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace HotChocolate.CodeGeneration.EntityFramework
{
    public partial class EntityFrameworkCodeGenerator : ICodeGenerator
    {
        public CodeGenerationResult Generate(CodeGeneratorContext context)
        {
            var result = new CodeGenerationResult();

            ISchema schema = EntityFrameworkSchemaHelper.CreateSchema(context.Documents);

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
            var @namespace = generatorContext.Namespace;

            var typeName = "AppDbContext";

            var usePluralizedTableNames = true; // TODO: Grab from a @tableNamingConvention or something directive on Schema type.

            ClassDeclarationSyntax dbContextClass =
                ClassDeclaration(typeName)
                    .AddGeneratedAttribute()
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword)); // TODO: Should it be partial?

            var objectTypes = schema.Types
                .OfType<ObjectType>()
                .Where(type => !IntrospectionTypes.IsIntrospectionType(type))
                .ToList();

            foreach (ObjectType objectType in objectTypes)
            {
                GenerateModel(
                    result,
                    usePluralizedTableNames: usePluralizedTableNames,
                    ref dbContextClass,
                    @namespace,
                    objectType);
            }

            // Generate DbContext class
            result.AddClass(@namespace, "AppDbContext", dbContextClass);
        }

        private static void GenerateModel(
            CodeGenerationResult result,
            bool usePluralizedTableNames,
            ref ClassDeclarationSyntax dbContextClass,
            string @namespace,
            IObjectType objectType)
        {
            TypeNameDirective? typeNameDirective =
                objectType.GetFirstDirective<TypeNameDirective>("typeName");
            var modelName = typeNameDirective?.Name ?? objectType.Name.Value;
            var modelConfigurerName = $"{modelName}Configurer";

            TableDirective? tableDirective =
                objectType.GetFirstDirective<TableDirective>(TableDirectiveType.NameConst);
            JsonDirective? jsonDirective =
                objectType.GetFirstDirective<JsonDirective>(JsonDirectiveType.NameConst);

            var tableName = GetTableName(objectType, tableDirective, jsonDirective, usePluralizedTableNames);
            var hasTable = tableName != null;
            var singularName = tableName.Singularize(inputIsKnownToBePlural: usePluralizedTableNames);
            var pluralizedName = tableName.Pluralize(inputIsKnownToBeSingular: !usePluralizedTableNames);

            ClassDeclarationSyntax modelDeclaration =
                ClassDeclaration(modelName)
                    .AddGeneratedAttribute()
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword));

            ClassDeclarationSyntax modelConfigurerDeclaration =
                ClassDeclaration(modelConfigurerName)
                    .AddGeneratedAttribute()
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword))
                    .WithBaseList(GetModelConfigurerBaseList(modelName))
                    .WithMembers(GetModelConfigurerConfigureMethod(modelName));

            foreach (IEntityFrameworkDirective directive in objectType.Directives
                .OfType<IEntityFrameworkDirective>())
            {

            }

            foreach (IObjectField field in objectType.Fields.Where(t => !t.IsIntrospectionField))
            {
                modelDeclaration =
                    modelDeclaration.AddProperty(
                        field.GetPropertyName(),
                        IdentifierName(field.GetTypeName(@namespace)),
                        field.Description,
                        setable: true);
            }

            // Generate model and model configurer classes
            result.AddClass(@namespace, modelName, modelDeclaration);
            result.AddClass(@namespace, modelConfigurerName, modelConfigurerDeclaration, _modelConfigurerUsings);

            // Add it on to the DbContext
            if (hasTable)
            {
                GenericNameSyntax? dbSetType = GetDbSetTypeName(@namespace, modelName);
                dbContextClass = dbContextClass.AddProperty(
                    pluralizedName,
                    dbSetType,
                    description: null,
                    setable: true);
            }
        }

        private static string? GetTableName(
            IObjectType objectType,
            TableDirective? tableDirective,
            JsonDirective? jsonDirective,
            bool usePluralizedTableNames)
        {
            // If it's got a json directive, it's a nested entity
            if (jsonDirective is not null)
            {
                return null;
            }

            // If there's a table directive explicitly stating a name, use that
            if (tableDirective is not null &&
                !string.IsNullOrWhiteSpace(tableDirective.Name))
            {
                return tableDirective.Name;
            }

            // Else derive something from the type's name
            var tableName = objectType.Name.Value;
            if (usePluralizedTableNames)
            {
                tableName = tableName.Pluralize(inputIsKnownToBeSingular: false);
            }
            return tableName;
        }

        private static readonly QualifiedNameSyntax _msEfCoreQualifiedNameForNs =
            QualifiedName(
                IdentifierName("Microsoft"),
                IdentifierName("EntityFrameworkCore"));

        private static readonly QualifiedNameSyntax _msEfCoreQualifiedName =
            QualifiedName(
                AliasQualifiedName(
                    IdentifierName(Token(SyntaxKind.GlobalKeyword)),
                    IdentifierName("Microsoft")),
                IdentifierName("EntityFrameworkCore"));

        private static readonly GenericNameSyntax _dbSetGenericName =
            GenericName(Identifier("DbSet"));

        private static readonly UsingDirectiveSyntax[] _modelConfigurerUsings = new[]
        {
            UsingDirective(_msEfCoreQualifiedNameForNs),
            UsingDirective(
                QualifiedName(
                    QualifiedName(
                        _msEfCoreQualifiedNameForNs,
                        IdentifierName("Metadata")),
                    IdentifierName("Builders")))
        };

        private static BaseListSyntax GetModelConfigurerBaseList(string modelTypeName) =>
            BaseList(
                SingletonSeparatedList<BaseTypeSyntax>(
                    SimpleBaseType(
                        GenericName(
                            Identifier("IEntityTypeConfiguration"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(modelTypeName)))))));

        private static SyntaxList<MemberDeclarationSyntax> GetModelConfigurerConfigureMethod(string modelTypeName) =>
            SingletonList<MemberDeclarationSyntax>(
                MethodDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.VoidKeyword)),
                    Identifier("Configure"))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                Identifier("builder"))
                            .WithType(
                                GenericName(
                                    Identifier("EntityTypeBuilder"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            IdentifierName(modelTypeName))))))))
                .WithBody(
                    Block()));

        private static GenericNameSyntax GetDbSetTypeName(string @namespace, string modelTypeName)
        {
            IdentifierNameSyntax fullModelTypeName =
                IdentifierName(Global(@namespace + "." + modelTypeName));

            return _dbSetGenericName.WithTypeArgumentList(
                TypeArgumentList(
                    SingletonSeparatedList<TypeSyntax>(fullModelTypeName)));
        }
    }
}
