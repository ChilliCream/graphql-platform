using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Analyzers.Configuration;
using HotChocolate.Analyzers.Diagnostics;
using HotChocolate.Analyzers.Types;
using HotChocolate.Analyzers.Types.EFCore;
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
    public partial class EFCoreSourceGenerator : ISourceGenerator
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
                    if (config.Extensions.EF is not null &&
                        context.GetSchemaDocuments(config) is { Count: > 0 } schemaDocuments)
                    {
                        ISchema schema = SchemaHelper.CreateEFCoreSchema(schemaDocuments);
                        DataGeneratorContext dataContext = DataGeneratorContext.FromSchema(schema);
                        GenerateTypes(context, dataContext, config.Extensions.EF, schema);
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
            EFCoreSettings settings,
            ISchema schema)
        {
            foreach (ObjectType objectType in schema.Types
                .OfType<ObjectType>()
                .Where(type => !IntrospectionTypes.IsIntrospectionType(type))
                .ToList())
            {
                GenerateModel(context, settings.Namespace, objectType); // TODO: There's a warning here about namespace being nullable, but it's cuz the setting type has it nullable. What should it be? Neo4J generator does ! which seems wrong
            }
        }

        private static void GenerateModel(
            GeneratorExecutionContext context,
            string @namespace,
            IObjectType objectType)
        {
            TypeNameDirective? typeNameDirective =
                objectType.GetFirstDirective<TypeNameDirective>("typeName");
            var modelName = typeNameDirective?.Name ?? objectType.Name.Value;
            var modelConfigurerName = $"{modelName}Configurer";

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
            context.AddClass(@namespace, modelName, modelDeclaration, _emptyUsings);
            //context.AddClass(@namespace, modelConfigurerName, modelConfigurerDeclaration, _modelConfigurerUsings);
        }

        private static readonly SyntaxList<UsingDirectiveSyntax> _emptyUsings = new();

        private static readonly QualifiedNameSyntax _msEfCoreQualifiedName = 
            QualifiedName(
                IdentifierName("Microsoft"),
                IdentifierName("EntityFrameworkCore"));

        private static readonly SyntaxList<UsingDirectiveSyntax> _modelConfigurerUsings = new()
        {
            UsingDirective(_msEfCoreQualifiedName),
            UsingDirective(
                QualifiedName(
                    QualifiedName(
                        _msEfCoreQualifiedName,
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
                        SingletonSeparatedList<ParameterSyntax>(
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
    }

    public static class ExtensionsToRefactorElsewhere
    {
        public static void AddClass(
            this GeneratorExecutionContext context,
            string @namespace,
            string className,
            ClassDeclarationSyntax classDeclaration,
            SyntaxList<UsingDirectiveSyntax> usings)
        {
            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(@namespace))
                    .AddMembers(classDeclaration);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration)
                    .WithUsings(usings);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            context.AddSource(@namespace + $".{className}.cs", compilationUnit.ToFullString());
        }
    }
}
