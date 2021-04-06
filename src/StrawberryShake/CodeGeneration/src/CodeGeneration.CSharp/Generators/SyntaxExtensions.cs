using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
            where TMember: MemberDeclarationSyntax
        {
            if (value is { Length: > 0 })
            {
                return member.AddSimple(XmlSummaryElement(XmlText(value)));
            }

            return member;
        }
    }
}
