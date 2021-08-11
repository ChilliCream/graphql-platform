using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HotChocolate.CodeGeneration.EntityFramework
{
    public static class SyntaxConstants
    {
        public static readonly QualifiedNameSyntax MsEfCoreQualifiedNameForNs =
            QualifiedName(
                IdentifierName("Microsoft"),
                IdentifierName("EntityFrameworkCore"));

        public static readonly QualifiedNameSyntax MsEfCoreQualifiedName =
            QualifiedName(
                AliasQualifiedName(
                    IdentifierName(Token(SyntaxKind.GlobalKeyword)),
                    IdentifierName("Microsoft")),
                IdentifierName("EntityFrameworkCore"));

        public static readonly GenericNameSyntax DbSetGenericName =
            GenericName(Identifier("DbSet"));

        public static readonly UsingDirectiveSyntax[] ModelConfigurerUsings = new[]
        {
            UsingDirective(MsEfCoreQualifiedNameForNs),
            UsingDirective(
                QualifiedName(
                    QualifiedName(
                        MsEfCoreQualifiedNameForNs,
                        IdentifierName("Metadata")),
                    IdentifierName("Builders")))
        };
    }
}
