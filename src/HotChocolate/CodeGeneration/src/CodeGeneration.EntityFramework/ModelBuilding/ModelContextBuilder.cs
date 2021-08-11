using System;
using System.Linq;
using HotChocolate.CodeGeneration.EntityFramework.Types;
using HotChocolate.CodeGeneration.Types;
using HotChocolate.Types;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static HotChocolate.CodeGeneration.EntityFramework.SyntaxConstants;

namespace HotChocolate.CodeGeneration.EntityFramework.ModelBuilding
{
    public static partial class ModelContextBuilder
    {
        private static readonly string[] IdFieldNames = new[]
        {
            "Id",
            "ID",
            "id"
        };

        public static void Process(ModelContext context)
        {
            ObjectType objectType = context.ObjectType;

            // Determine naming
            TypeNameDirective? typeNameDirective =
                objectType.GetFirstDirective<TypeNameDirective>("typeName");
            context.ModelName = typeNameDirective?.Name ?? objectType.Name.Value;
            context.ModelNamePluralized = typeNameDirective?.PluralName ?? objectType.Name.Value.Pluralize();

            // Determine if table
            JsonDirective? jsonDirective =
                objectType.GetFirstDirective<JsonDirective>(JsonDirectiveType.NameConst);
            context.IsBackedByTable = jsonDirective is null;

            if (context.IsBackedByTable)
            {
                // Primary key
                SetPrimaryKey(context, objectType);
            }

            // Model
            SetModelClass(context);

            if (context.IsBackedByTable)
            {
                // Configurer
                SetModelConfigurerClass(context);
            }
        }

        private static void SetPrimaryKey(ModelContext context, ObjectType objectType)
        {
            IObjectField[] fieldsMarkedAsKey = objectType.Fields
                            .Where(f => f.Directives.Any(d => d is KeyDirective))
                            .ToArray();
            if (fieldsMarkedAsKey.Any())
            {
                context.PrimaryKey = fieldsMarkedAsKey.Select(f => new PrimaryKeyField(f)).ToArray();
            }
            else // try find one
            {
                var possibleKeyFieldNames = IdFieldNames
                    .Concat(IdFieldNames.Select(name => $"{context.ModelName}{name}"))
                    .ToArray();

                IObjectField[] possibleKeyFields = objectType.Fields
                    .Where(f => possibleKeyFieldNames.Contains(f.Name.Value))
                    .ToArray();

                if (possibleKeyFields.Length > 1)
                {
                    throw new Exception(
                        $"Multiple key possibilities found. " +
                        $"Candidates: {string.Join(", ", possibleKeyFields.Select(f => f.Name.Value))}");
                }

                if (possibleKeyFields.Length == 1)
                {
                    context.PrimaryKey = possibleKeyFields.Select(f => new PrimaryKeyField(f)).ToArray();
                }
                else // create one from convention
                {
                    context.PrimaryKey = new[]
                    {
                        new PrimaryKeyField(
                            context.Conventions.GeneratedIdFieldName,
                        context.Conventions.GeneratedIdRuntimeType)
                    };
                }
            }
        }

        private static void SetModelClass(ModelContext context)
        {
            context.ModelClass =
                ClassDeclaration(context.RequiredModelName)
                    .AddGeneratedAttribute()
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword));

            foreach (IObjectField field in context.ObjectType.Fields.Where(t => !t.IsIntrospectionField))
            {
                context.ModelClass =
                    context.ModelClass.AddProperty(
                        field.GetPropertyName(),
                        IdentifierName(field.GetTypeName(context.Namespace)),
                        field.Description,
                        setable: true);
            }
        }

        private static void SetModelConfigurerClass(ModelContext context)
        {
            context.ModelConfigurerClass =
                ClassDeclaration(context.RequiredModelConfigurerName)
                    .AddGeneratedAttribute()
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword))
                    .WithBaseList(GetModelConfigurerBaseList(context.RequiredModelName));

            SyntaxList<StatementSyntax> configurationStatements;

            context.TableDirective =
                context.ObjectType.GetFirstDirective<TableDirective>(TableDirectiveType.NameConst);

            // Configure the table name explicitly if needed
            // (EF uses the DbSet property's name by default, which we always set as modelNamePluralized)
            var tableName = context.TableDirective?.Name
                ?? (context.Conventions.UsePluralizedTableNames
                    ? context.RequiredModelNamePluralized
                    : context.RequiredModelName);
            if (tableName != context.RequiredModelNamePluralized)
            {
                configurationStatements.Add(GetTableNameConfigurationExpression(tableName));
            }

            // Run through other model configuring directives and build up statements 
            foreach (IModelConfiguringDirective directive in context.ObjectType.Directives
                .OfType<IModelConfiguringDirective>())
            {
                configurationStatements.Add(directive.AsConfigurationStatement());
            }

            // Build and add the Configure method
            MemberDeclarationSyntax configureMethod = GetModelConfigurerConfigureMethod(
                context.RequiredModelName,
                configurationStatements);

            context.ModelConfigurerClass = context.ModelConfigurerClass
                .WithMembers(SingletonList(configureMethod));
        }

        private static BaseListSyntax GetModelConfigurerBaseList(string modelTypeName) =>
            BaseList(
                SingletonSeparatedList<BaseTypeSyntax>(
                    SimpleBaseType(
                        QualifiedName(
                            EFCoreQualifiedName,
                            GenericName(
                                Identifier("IEntityTypeConfiguration"),
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(modelTypeName))))))));

        private const string BuilderArgumentName = "builder";

        private static MemberDeclarationSyntax GetModelConfigurerConfigureMethod(
            string modelTypeName,
            SyntaxList<StatementSyntax> statements) =>
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
                            Identifier(BuilderArgumentName))
                        .WithType(
                            QualifiedName(
                                EFCoreMetadataBuildersQualifiedName,
                                GenericName(
                                    Identifier("EntityTypeBuilder"),
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            IdentifierName(modelTypeName)))))))))
            .WithBody(
                Block().WithStatements(statements));

        private static ExpressionStatementSyntax GetTableNameConfigurationExpression(
            string tableName) =>
            ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(BuilderArgumentName),
                        IdentifierName("ToTable")))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(tableName)))))));
    }
}
