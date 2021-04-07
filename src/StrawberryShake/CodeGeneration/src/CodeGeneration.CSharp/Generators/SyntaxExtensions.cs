using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public static class SyntaxExtensions
    {
        public static PropertyDeclarationSyntax WithGetterAndSetter(
            this PropertyDeclarationSyntax property) =>
                property.AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

        public static PropertyDeclarationSyntax WithGetterAndInit(
            this PropertyDeclarationSyntax property) =>
            property.AddAccessorListAccessors(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

        public static PropertyDeclarationSyntax WithGetter(
            this PropertyDeclarationSyntax property) =>
            property.AddAccessorListAccessors(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

        public static PropertyDeclarationSyntax WithSuppressNullableWarningExpression(
            this PropertyDeclarationSyntax property) =>
            property
                .WithInitializer(
                    EqualsValueClause(
                        PostfixUnaryExpression(
                            SyntaxKind.SuppressNullableWarningExpression,
                            LiteralExpression(
                                SyntaxKind.DefaultLiteralExpression,
                                Token(SyntaxKind.DefaultKeyword)))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        private static TMember AddSimple<TMember>(
            this TMember member,
            XmlElementSyntax xmlElement)
            where TMember : MemberDeclarationSyntax
        {
            return member.WithLeadingTrivia(
                TriviaList(
                    Trivia(
                        DocumentationComment(
                            xmlElement,
                            XmlText().WithTextTokens(
                                TokenList(
                                    Token(
                                        TriviaList(),
                                        SyntaxKind.XmlTextLiteralNewLineToken,
                                        System.Environment.NewLine,
                                        System.Environment.NewLine,
                                        TriviaList())))))));
        }

        public static TMember AddSummary<TMember>(
            this TMember member,
            string? value)
            where TMember : MemberDeclarationSyntax
        {
            if (value is { Length: > 0 })
            {
                return member.AddSimple(XmlSummaryElement(XmlText(value)));
            }

            return member;
        }

        public static T AddGeneratedAttribute<T>(this T type) where T : BaseTypeDeclarationSyntax
        {
            var version = typeof(SyntaxExtensions).Assembly.GetName().Version!.ToString();

            AttributeSyntax attribute =
                Attribute(
                    QualifiedName(
                        QualifiedName(
                            QualifiedName(
                                AliasQualifiedName(
                                    IdentifierName(
                                        Token(SyntaxKind.GlobalKeyword)),
                                    IdentifierName("System")),
                                IdentifierName("CodeDom")),
                            IdentifierName("Compiler")),
                        IdentifierName("GeneratedCode")))
                    .AddArgumentListArguments(
                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal("StrawberryShake"))))
                    .AddArgumentListArguments(
                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(version))));


            return (T)type
                .WithAttributeLists(
                    SingletonList(
                        AttributeList(
                            SingletonSeparatedList(
                                attribute))));
        }

        public static T AddImplements<T>(
            this T type,
            IReadOnlyList<string> implements)
            where T : TypeDeclarationSyntax
        {
            if (implements.Count == 0)
            {
                return type;
            }

            return (T)type.AddBaseListTypes(
                implements
                    .Select(CreateDataTypeName)
                    .Select(t => SimpleBaseType(IdentifierName(t)))
                    .ToArray());
        }

        public static T AddStateProperty<T>(
            this T type,
            PropertyDescriptor property)
            where T : TypeDeclarationSyntax =>
            AddProperty(
                type,
                property.Name,
                property.Type.ToStateTypeSyntax(),
                property.Description);

        public static T AddTypeProperty<T>(
            this T type)
            where T : TypeDeclarationSyntax =>
            AddProperty(
                type,
                WellKnownNames.TypeName,
                ParseTypeName(TypeNames.String),
                null);

        public static T AddProperty<T>(
            this T type,
            string name,
            TypeSyntax typeSyntax,
            string? description)
            where T : TypeDeclarationSyntax
        {
            PropertyDeclarationSyntax propertyDeclaration =
                PropertyDeclaration(typeSyntax, name);

            if (type is not InterfaceDeclarationSyntax)
            {
                propertyDeclaration = propertyDeclaration
                    .AddModifiers(Token(SyntaxKind.PublicKeyword));
            }

            propertyDeclaration = propertyDeclaration.AddSummary(description);

            propertyDeclaration = type is RecordDeclarationSyntax
                ? propertyDeclaration.WithGetterAndInit()
                : propertyDeclaration.WithGetter();

            return (T)type.AddMembers(propertyDeclaration);
        }

        public static ConstructorDeclarationSyntax AddStateParameter(
            this ConstructorDeclarationSyntax constructor,
            PropertyDescriptor property) =>
            AddParameter(constructor, property.Name, property.Type.ToStateTypeSyntax());

        public static ConstructorDeclarationSyntax AddTypeParameter(
            this ConstructorDeclarationSyntax constructor) =>
            AddParameter(
                constructor,
                WellKnownNames.TypeName,
                ParseTypeName(TypeNames.String),
                true);

        public static ConstructorDeclarationSyntax AddParameter(
            this ConstructorDeclarationSyntax constructor,
            string name,
            TypeSyntax typeSyntax,
            bool assertNotNull = false)
        {
            string paramName;
            string propertyName;

            if (name == WellKnownNames.TypeName)
            {
                paramName = WellKnownNames.TypeName;
                propertyName = $"this.{WellKnownNames.TypeName}";
            }
            else
            {
                paramName = GetParameterName(name);
                propertyName = name;
            }

            return constructor
                .AddParameterListParameters(
                    Parameter(Identifier(paramName))
                        .WithType(typeSyntax))
                .AssignParameter(propertyName, paramName, assertNotNull);
        }

        public static ConstructorDeclarationSyntax AssignParameter(
            this ConstructorDeclarationSyntax constructor,
            string propertyName,
            string parameterName,
            bool assertNotNull = false)
        {
            BinaryExpressionSyntax assertNotNullExpression =
                BinaryExpression(
                    SyntaxKind.CoalesceExpression,
                    IdentifierName(parameterName),
                    ThrowExpression(
                        ObjectCreationExpression(IdentifierName(TypeNames.ArgumentNullException))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        InvocationExpression(IdentifierName("nameof"))
                                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                            Argument(IdentifierName(parameterName)))))))))));


            AssignmentExpressionSyntax assignmentExpression =
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(propertyName),
                    assertNotNull
                        ? assertNotNullExpression
                        : IdentifierName(parameterName));

            return constructor.AddBodyStatements(ExpressionStatement(assignmentExpression));
        }
    }
}
