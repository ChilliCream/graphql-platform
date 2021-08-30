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
using static HotChocolate.CodeGeneration.TypeNames;
using System.Collections.Generic;

namespace HotChocolate.CodeGeneration.EntityFramework.ModelBuilding
{
    public static partial class EntityBuilder
    {
        private const string x = "x";

        public static void Process(EntityBuilderContext context)
        {
            ObjectType objectType = context.ObjectType;

            // Determine naming
            TypeNameDirective? typeNameDirective =
                objectType.GetFirstDirective<TypeNameDirective>("typeName");
            context.EntityName = typeNameDirective?.Name ?? objectType.Name.Value;
            context.EntityNamePluralized = typeNameDirective?.PluralName ?? objectType.Name.Value.Pluralize();

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

        private static void SetPrimaryKey(EntityBuilderContext context, ObjectType objectType)
        {
            IObjectField[] fieldsMarkedAsKey = objectType.Fields
                .Where(f => f.GetFirstDirective<KeyDirective>(KeyDirectiveType.NameConst) is not null)
                .ToArray();
            if (fieldsMarkedAsKey.Any())
            {
                var nullabilityViolations = fieldsMarkedAsKey
                    .Where(f => f.Type.IsNullableType())
                    .Select(f => f.Name.Value)
                    .ToArray();
                if (nullabilityViolations.Any())
                {
                    throw new Exception(
                        $"@key can't be applied to a nullable field. " +
                        $"Violating field/s: {string.Join(", ", nullabilityViolations)}.");
                }

                try
                {
                    var pkName = fieldsMarkedAsKey
                        .Select(f => f.GetFirstDirective<KeyDirective>(KeyDirectiveType.NameConst)!.Name)
                        .Distinct()
                        .Single();
                    if (pkName is not null)
                    {
                        context.PrimaryKeyName = pkName;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    throw new Exception(
                        $"@key can't be applied to multiple fields in the same different " +
                        $"with a varying custom PK name.",
                        ex);
                }

                context.PrimaryKeyColumns = fieldsMarkedAsKey
                    .Select(f => new PrimaryKeyColumn(f))
                    .ToArray();
            }
            else // try find one
            {
                var possibleKeyFieldNames = new[]
                {
                    "id",
                    $"{context.EntityName}id"
                };

                IObjectField[] possibleKeyFields = objectType.Fields
                    .Where(f =>
                        possibleKeyFieldNames
                            .Any(pkfn => pkfn.Equals(f.Name.Value, StringComparison.OrdinalIgnoreCase)) &&
                        !f.Type.IsNullableType())
                    .ToArray();

                if (possibleKeyFields.Length > 1)
                {
                    throw new Exception(
                        $"Multiple key possibilities found. " +
                        $"Candidates: {string.Join(", ", possibleKeyFields.Select(f => f.Name.Value))}");
                }

                if (possibleKeyFields.Length == 1)
                {
                    context.PrimaryKeyColumns = possibleKeyFields.Select(f => new PrimaryKeyColumn(f)).ToArray();
                }
                else // create one from convention
                {
                    context.PrimaryKeyColumns = new[]
                    {
                        new PrimaryKeyColumn(context.Conventions.GeneratedIdRuntimeType)
                    };
                }
            }
        }

        private static void SetModelClass(EntityBuilderContext context)
        {
            context.EntityClass =
                ClassDeclaration(context.RequiredEntityName)
                    .AddGeneratedAttribute()
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword));

            // Auto-add a PK property if the type doesn't have a field for it
            foreach (PrimaryKeyColumn pk in context.RequiredPrimaryKeyColumns)
            {
                if (pk.Field is null)
                {
                    context.EntityClass =
                       context.EntityClass.AddProperty(
                           context.Conventions.GeneratedIdPropertyName,
                           IdentifierName(pk.RuntimeType.ToGlobalTypeName()),
                           description: null,
                           setable: true);
                }
            }

            foreach (IObjectField field in context.ObjectType.Fields.Where(t => !t.IsIntrospectionField))
            {
                context.EntityClass =
                    context.EntityClass.AddProperty(
                        field.GetPropertyName(),
                        IdentifierName(field.GetTypeName(context.Namespace)),
                        field.Description,
                        setable: true);
            }
        }

        private static void SetModelConfigurerClass(EntityBuilderContext context)
        {
            context.EntityConfigurerClass =
                ClassDeclaration(context.RequiredEntityConfigurerName)
                    .AddGeneratedAttribute()
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword))
                    .WithBaseList(GetModelConfigurerBaseList(context.RequiredEntityName));

            var configurationStatements = new List<StatementSyntax>();

            context.TableDirective =
                context.ObjectType.GetFirstDirective<TableDirective>(TableDirectiveType.NameConst);

            // Configure the table name explicitly if needed
            // (EF uses the DbSet property's name by default, which we always set as modelNamePluralized)
            var tableName = context.TableDirective?.Name
                ?? (context.Conventions.UsePluralizedTableNames
                    ? context.RequiredEntityNamePluralized
                    : context.RequiredEntityName);
            if (tableName != context.RequiredEntityNamePluralized)
            {
                configurationStatements.Add(GetTableNameConfigurationExpression(tableName));
            }

            // Configure the PK
            configurationStatements.Add(GetPrimaryKeyConfigurationExpression(context));

            // Run through other model configuring directives and build up statements 
            foreach (IModelConfiguringDirective directive in context.ObjectType.Directives
                .OfType<IModelConfiguringDirective>())
            {
                configurationStatements.Add(directive.AsConfigurationStatement());
            }

            // Build and add the Configure method
            MemberDeclarationSyntax configureMethod = GetModelConfigurerConfigureMethod(
                context.RequiredEntityName,
                configurationStatements.ToArray());

            context.EntityConfigurerClass = context.EntityConfigurerClass
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
            StatementSyntax[] statements) =>
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
            .AddBodyStatements(statements);

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

        private static ExpressionStatementSyntax GetPrimaryKeyConfigurationExpression(
            EntityBuilderContext context)
        {
            PrimaryKeyColumn[]? primaryKeyColumns = context.RequiredPrimaryKeyColumns;
            
            ExpressionSyntax keySelectorExprBody;

            if (primaryKeyColumns.Length == 1)
            {
                // .HasKey(x => x.Id)

                PrimaryKeyColumn pkCol = primaryKeyColumns[0];
                keySelectorExprBody =
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(x),
                        IdentifierName(
                            pkCol.Field?.GetPropertyName()
                                ?? context.Conventions.GeneratedIdPropertyName));
            }
            else
            {
                // .HasKey(x => new { x.FooId, x.BarId })

                var bits = new List<SyntaxNodeOrToken>(primaryKeyColumns.Length * 2 - 1);
                PrimaryKeyColumn lastPkCol = primaryKeyColumns.Last();
                foreach (PrimaryKeyColumn? pkCol in primaryKeyColumns)
                {
                    // x.FooId
                    bits.Add(
                        AnonymousObjectMemberDeclarator(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(x),
                                IdentifierName(
                                    pkCol.Field?.GetPropertyName()
                                        ?? context.Conventions.GeneratedIdPropertyName))));

                    // ,
                    if (pkCol != lastPkCol)
                    {
                        bits.Add(Token(SyntaxKind.CommaToken));
                    }
                }

                keySelectorExprBody =
                    AnonymousObjectCreationExpression(
                        SeparatedList<AnonymousObjectMemberDeclaratorSyntax>(
                            bits));
            }

            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(BuilderArgumentName),
                        IdentifierName("HasKey")))
                .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    SimpleLambdaExpression(
                                        Parameter(
                                            Identifier(x)))
                                    .WithExpressionBody(keySelectorExprBody))))));
        }
    }
}
