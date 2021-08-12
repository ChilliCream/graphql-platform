using System.Linq;
using HotChocolate.CodeGeneration.EntityFramework.ModelBuilding;
using HotChocolate.CodeGeneration.EntityFramework.Types;
using HotChocolate.CodeGeneration.Types;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
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
        private static readonly string[] _operationTypeNames = new[]
        {
            OperationTypeNames.Query,
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

            var dbContextClassName = schemaConventions.DbContextName;

            ClassDeclarationSyntax dbContextClass = GenerateDbContext(
                dbContextClassName,
                schema);

            // Process models
            var objectTypes = schema.Types
                .OfType<ObjectType>()
                .Where(type =>
                    !IntrospectionTypes.IsIntrospectionType(type) &&
                    !_operationTypeNames.Contains(type.Name.Value))
                .ToList();

            foreach (ObjectType objectType in objectTypes)
            {
                var modelContext = new ModelContext(
                    schemaConventions,
                    @namespace,
                    objectType);

                ModelContextBuilder.Process(modelContext);

                // Model
                if (modelContext.ModelClass is not null)
                {
                    result.AddClass(
                        modelContext.Namespace,
                        modelContext.RequiredModelName,
                        modelContext.ModelClass);
                }

                // Configurer
                if (modelContext.ModelConfigurerClass is not null)
                {
                    result.AddClass(
                        modelContext.Namespace,
                        modelContext.RequiredModelConfigurerName,
                        modelContext.ModelConfigurerClass,
                        modelContext.ModelConfigurerUsings);
                }

                // DbSet
                if (modelContext.IsBackedByTable)
                {
                    QualifiedNameSyntax? dbSetType = GetDbSetTypeName(@namespace, modelContext.RequiredModelName);
                    dbContextClass = dbContextClass.AddProperty(
                        modelContext.RequiredModelNamePluralized,
                        dbSetType,
                        description: null,
                        setable: true);
                }
            }

            // Generate DbContext class
            result.AddClass(@namespace, dbContextClassName, dbContextClass);
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
    }
}
