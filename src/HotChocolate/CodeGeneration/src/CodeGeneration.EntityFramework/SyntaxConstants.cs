using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HotChocolate.CodeGeneration.EntityFramework
{
    public static class SyntaxConstants
    {
        public static readonly QualifiedNameSyntax EFCoreQualifiedNameForNs =
            QualifiedName(
                IdentifierName("Microsoft"),
                IdentifierName("EntityFrameworkCore"));

        public static readonly QualifiedNameSyntax EFCoreQualifiedName =
            QualifiedName(
                AliasQualifiedName(
                    IdentifierName(Token(SyntaxKind.GlobalKeyword)),
                    IdentifierName("Microsoft")),
                IdentifierName("EntityFrameworkCore"));

        public static readonly QualifiedNameSyntax EFCoreMetadataBuildersQualifiedNameForNs =
            QualifiedName(
                QualifiedName(
                    EFCoreQualifiedNameForNs,
                    IdentifierName("Metadata")),
                IdentifierName("Builders"));

        public static readonly QualifiedNameSyntax EFCoreMetadataBuildersQualifiedName =
            QualifiedName(
                QualifiedName(
                    EFCoreQualifiedName,
                    IdentifierName("Metadata")),
                IdentifierName("Builders"));

        public static readonly QualifiedNameSyntax DbContextQualifiedName =
            QualifiedName(
                EFCoreQualifiedName,
                IdentifierName("DbContext"));

        public static readonly BaseListSyntax DbContextBaseList =
            BaseList(
                SingletonSeparatedList<BaseTypeSyntax>(
                    SimpleBaseType(DbContextQualifiedName)));

        public static readonly GenericNameSyntax DbSetGenericName =
            GenericName(Identifier("DbSet"));

        public static readonly UsingDirectiveSyntax[] ModelConfigurerUsings = new[]
        {
            UsingDirective(EFCoreQualifiedNameForNs),
            UsingDirective(EFCoreMetadataBuildersQualifiedNameForNs)
        };
    }
}
