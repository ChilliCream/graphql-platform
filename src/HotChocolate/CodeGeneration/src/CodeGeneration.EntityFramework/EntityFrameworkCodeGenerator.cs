using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.CodeGeneration.DependencyInjection;
using HotChocolate.CodeGeneration.EntityFramework.ModelBuilding;
using HotChocolate.CodeGeneration.EntityFramework.Types;
using HotChocolate.CodeGeneration.Types;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.CodeGeneration.EntityFramework.SyntaxConstants;
using static HotChocolate.CodeGeneration.TypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace HotChocolate.CodeGeneration.EntityFramework
{
    public class EntityFrameworkCodeGenerator : ICodeGenerator
    {
        // TODO: Review this. Are these actually reserved in the spec or can someone
        // define whatever type name they want in which case we need to be smarter and crawl?
        // (There's a todo in the Neo4J generator to take it from config)
        private static readonly string _queryTypeName = OperationTypeNames.Query;
        private static readonly string[] _operationTypeNames = new[]
        {
            _queryTypeName,
            OperationTypeNames.Mutation,
            OperationTypeNames.Subscription
        };

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

            SchemaConventionsDirective schemaConventions = schema.GetFirstDirective(
                SchemaConventionsDirective.DirectiveName,
                new SchemaConventionsDirective())
                ?? new SchemaConventionsDirective();

            List<EntityBuilderContext> entities =
                GenerateEFModel(result, schema, @namespace, schemaConventions);

            GenerateQueryType(result, dataContext, generatorContext, entities);

            GenerateDependencyInjectionCode(result, generatorContext);
        }

        private static List<EntityBuilderContext> GenerateEFModel(
            CodeGenerationResult result,
            ISchema schema,
            string @namespace,
            SchemaConventionsDirective schemaConventions)
        {
            var modelBuilderContext = new ModelBuilderContext(
                schemaConventions,
                @namespace);

            var dbContextClassName = schemaConventions.DbContextName;

            ClassDeclarationSyntax dbContextClass = GenerateDbContext(
                dbContextClassName,
                schema);

            // Process object types
            var objectTypes = schema.Types
                .OfType<ObjectType>()
                .Where(type =>
                    !IntrospectionTypes.IsIntrospectionType(type) &&
                    !_operationTypeNames.Contains(type.Name.Value))
                .ToList();

            var entities = new List<EntityBuilderContext>();

            foreach (ObjectType objectType in objectTypes)
            {
                var entityBuilderContext = new EntityBuilderContext(
                    modelBuilderContext,
                    objectType);

                EntityBuilder.Process(entityBuilderContext);

                // Entity
                if (entityBuilderContext.EntityClass is not null)
                {
                    result.AddClass(
                        @namespace,
                        entityBuilderContext.RequiredEntityName,
                        entityBuilderContext.EntityClass);
                }

                // DbSet
                if (entityBuilderContext.IsBackedByTable)
                {
                    QualifiedNameSyntax? dbSetType = GetDbSetTypeName(
                        @namespace,
                        entityBuilderContext.RequiredEntityName);

                    dbContextClass = dbContextClass.AddProperty(
                        entityBuilderContext.RequiredEntityNamePluralized,
                        dbSetType,
                        description: null,
                        setable: true);

                    entities.Add(entityBuilderContext);
                }
            }

            // Execute delayed processing, which can add more configuration statements per entity configurer class
            foreach ((ObjectType objectType, Action<EntityBuilderContext> processor) in modelBuilderContext.PostProcessors)
            {
                EntityBuilderContext entityBuilderContext = modelBuilderContext.EntityBuilderContexts[objectType];
                processor.Invoke(entityBuilderContext);
            }

            // Configurers
            foreach (ObjectType objectType in objectTypes)
            {
                EntityBuilderContext entityBuilderContext = modelBuilderContext.EntityBuilderContexts[objectType];
                if (entityBuilderContext.EntityConfigurerClass is not null)
                {
                    EntityBuilder.CompleteConfigurerClass(entityBuilderContext);
                    result.AddClass(
                        @namespace,
                        entityBuilderContext.RequiredEntityConfigurerName,
                        entityBuilderContext.EntityConfigurerClass,
                        entityBuilderContext.EntityConfigurerUsings);
                }
            }

            // Generate DbContext class
            result.AddClass(@namespace, dbContextClassName, dbContextClass);

            return entities;
        }

        private static ClassDeclarationSyntax GenerateDbContext(
            string dbContextClassName,
            ISchema schema)
        {
            ClassDeclarationSyntax dbContextClass = ClassDeclaration(dbContextClassName)
                .AddGeneratedAttribute()
                .WithBaseList(DbContextBaseList)
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword));

            // TODO: When needed
            //if (schemaType is not null)
            //{
            //    SyntaxList<StatementSyntax> configurationStatements;

            //    foreach (IDbContextConfiguringDirective? dbContextConfiguringDirective in schema
            //        .Directives
            //        .OfType<IDbContextConfiguringDirective>())
            //    {
            //        configurationStatements.Add(
            //            dbContextConfiguringDirective.AsConfigurationStatement());
            //    }

            //    // TODO: Generate an OnModelConfiguring method and add it
            //    //dbContextClass = dbContextClass.
            //}

            return dbContextClass;
        }

        private static QualifiedNameSyntax GetDbSetTypeName(string @namespace, string modelTypeName)
        {
            IdentifierNameSyntax fullModelTypeName =
                IdentifierName(Global(@namespace + "." + modelTypeName));

            return QualifiedName(
                EFCoreQualifiedName,
                DbSetGenericName.WithTypeArgumentList(
                    TypeArgumentList(
                        SingletonSeparatedList<TypeSyntax>(fullModelTypeName))));
        }

        private static void GenerateQueryType(
            CodeGenerationResult result,
            DataGeneratorContext dataContext,
            CodeGeneratorContext generatorContext,
            List<EntityBuilderContext> entities)
        {
            ClassDeclarationSyntax queryDeclaration =
                ClassDeclaration(_queryTypeName)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute()
                    .AddExtendObjectTypeAttribute(_queryTypeName);

            // TODO: Discuss what should be generated here...
            //foreach (EntityBuilderContext entity in entities)
            //{
            //    queryDeclaration = queryDeclaration.AddMembers(
            //        CreateQueryResolver(dataContext, generatorContext, entity));
            //}

            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(generatorContext.Namespace!))
                    .AddMembers(queryDeclaration);

            CompilationUnitSyntax compilationUnit =
                CompilationUnit()
                    .AddMembers(namespaceDeclaration);

            compilationUnit = compilationUnit.NormalizeWhitespace(elasticTrivia: true);

            result.AddClass(generatorContext.Namespace, _queryTypeName, compilationUnit.ToFullString());
        }

        //private static MethodDeclarationSyntax CreateQueryResolver(
        //    DataGeneratorContext dataContext,
        //    CodeGeneratorContext generatorContext,
        //    EntityBuilderContext entity)
        //{

        //}

        private static void GenerateDependencyInjectionCode(
            CodeGenerationResult result,
            CodeGeneratorContext generatorContext)
        {
            var additionalStatements = new List<StatementSyntax>
            {
                //AddEFCoreFiltering(),?
                //AddEFCoreSorting(),
                //AddEFCoreProjections()
            };

            DependencyInjectionHelper.GenerateDependencyInjectionCode(
                result,
                generatorContext,
                additionalStatements);
        }
    }
}
