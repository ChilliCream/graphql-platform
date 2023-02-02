using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using HotChocolate.CodeGeneration.Types;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static HotChocolate.CodeGeneration.TypeNames;

namespace HotChocolate.CodeGeneration;

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

    public static VariableDeclaratorSyntax WithSuppressNullableWarningExpression(
        this VariableDeclaratorSyntax variable) =>
        variable
            .WithInitializer(
                EqualsValueClause(
                    PostfixUnaryExpression(
                        SyntaxKind.SuppressNullableWarningExpression,
                        LiteralExpression(
                            SyntaxKind.DefaultLiteralExpression,
                            Token(SyntaxKind.DefaultKeyword)))));

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
                        XmlText()
                            .WithTextTokens(
                                TokenList(
                                    Token(
                                        TriviaList(),
                                        SyntaxKind.XmlTextLiteralNewLineToken,
                                        Environment.NewLine,
                                        Environment.NewLine,
                                        TriviaList())))))));
    }

    public static TMember AddSummary<TMember>(
        this TMember member,
        string? value)
        where TMember : MemberDeclarationSyntax
    {
        if (value is { Length: > 0 })
        {
            using var reader = new StringReader(value);
            var list = new List<XmlNodeSyntax>();
            string? line;

            do
            {
                line = reader.ReadLine();
                if (line is not null)
                {
                    list.Add(XmlText(line));
                }
            } while (line is not null);

            return member.AddSimple(XmlSummaryElement(list.ToArray()));
        }

        return member;
    }

    public static T AddGeneratedAttribute<T>(this T type)
        where T : BaseTypeDeclarationSyntax
    {
        var version = typeof(SyntaxExtensions).Assembly.GetName().Version!.ToString();

        var attribute =
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
                            Literal("HotChocolate"))))
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

    public static T AddExtendObjectTypeAttribute<T>(this T type, string typeName)
        where T : BaseTypeDeclarationSyntax
    {
        var attribute =
            Attribute(
                    QualifiedName(
                        QualifiedName(
                            AliasQualifiedName(
                                IdentifierName(
                                    Token(SyntaxKind.GlobalKeyword)),
                                IdentifierName("HotChocolate")),
                            IdentifierName("Types")),
                        IdentifierName("ExtendObjectType")))
                .AddArgumentListArguments(
                    AttributeArgument(
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(typeName))));

        return (T)type
            .WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(
                            attribute))));
    }

    public static T AddImplements<T>(
        this T type,
        params string[] implements)
        where T : TypeDeclarationSyntax
    {
        return type.AddImplements((IReadOnlyList<string>)implements);
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
                .Select(t => SimpleBaseType(IdentifierName(t)))
                .ToArray());
    }

    public static T AddProperty<T>(
        this T type,
        string name,
        TypeSyntax typeSyntax,
        string? description,
        bool setable = false,
        Func<PropertyDeclarationSyntax, PropertyDeclarationSyntax>? configure = null)
        where T : TypeDeclarationSyntax
    {
        var propertyDeclaration =
            PropertyDeclaration(typeSyntax, name);

        if (type is not InterfaceDeclarationSyntax)
        {
            propertyDeclaration = propertyDeclaration
                .AddModifiers(Token(SyntaxKind.PublicKeyword));
        }

        propertyDeclaration = propertyDeclaration.AddSummary(description);

        if (setable)
        {
            propertyDeclaration = propertyDeclaration.WithGetterAndSetter();
        }
        else
        {
            propertyDeclaration = type is RecordDeclarationSyntax
                ? propertyDeclaration.WithGetterAndInit()
                : propertyDeclaration.WithGetter();
        }

        if (configure is not null)
        {
            propertyDeclaration = configure(propertyDeclaration);
        }

        return (T)type.AddMembers(propertyDeclaration);
    }

    public static ConstructorDeclarationSyntax AssignParameter(
        this ConstructorDeclarationSyntax constructor,
        string propertyName,
        string parameterName,
        bool assertNotNull = false)
    {
        var assertNotNullExpression =
            BinaryExpression(
                SyntaxKind.CoalesceExpression,
                IdentifierName(parameterName),
                ThrowExpression(
                    ObjectCreationExpression(IdentifierName(
                            "global::" + typeof(ArgumentNullException).FullName))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        InvocationExpression(IdentifierName("nameof"))
                                            .WithArgumentList(ArgumentList(
                                                SingletonSeparatedList(
                                                    Argument(IdentifierName(
                                                        parameterName)))))))))));


        var assignmentExpression =
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(propertyName),
                assertNotNull
                    ? assertNotNullExpression
                    : IdentifierName(parameterName));

        return constructor.AddBodyStatements(ExpressionStatement(assignmentExpression));
    }

    public static MethodDeclarationSyntax AddPagingAttribute(
        this MethodDeclarationSyntax methodSyntax,
        PagingDirective directive)
    {
        if (directive.Kind == PagingKind.None)
        {
            return methodSyntax;
        }

        var attribute =
            Attribute(IdentifierName(Global(
                    directive.Kind == PagingKind.Cursor
                        ? UsePagingAttribute
                        : UseOffsetPagingAttribute)))
                .AddArgumentListArguments(
                    AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(directive.DefaultPageSize)))
                        .WithNameEquals(
                            NameEquals(IdentifierName(nameof(directive.DefaultPageSize)))),
                    AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(directive.MaxPageSize)))
                        .WithNameEquals(
                            NameEquals(IdentifierName(nameof(directive.MaxPageSize)))),
                    AttributeArgument(
                            LiteralExpression(
                                directive.IncludeTotalCount
                                    ? SyntaxKind.TrueLiteralExpression
                                    : SyntaxKind.FalseLiteralExpression))
                        .WithNameEquals(
                            NameEquals(IdentifierName(nameof(directive.IncludeTotalCount)))));

        return methodSyntax.AddAttributeLists(AttributeList(SingletonSeparatedList(attribute)));
    }

    public static MethodDeclarationSyntax AddFilteringAttribute(
        this MethodDeclarationSyntax methodSyntax)
    {
        var attribute =
            Attribute(IdentifierName(Global(UseFilteringAttribute)));

        return methodSyntax.AddAttributeLists(AttributeList(SingletonSeparatedList(attribute)));
    }

    public static MethodDeclarationSyntax AddSortingAttribute(
        this MethodDeclarationSyntax methodSyntax)
    {
        var attribute =
            Attribute(IdentifierName(Global(UseSortingAttribute)));

        return methodSyntax.AddAttributeLists(AttributeList(SingletonSeparatedList(attribute)));
    }

    public static MethodDeclarationSyntax AddProjectionAttribute(
        this MethodDeclarationSyntax methodSyntax)
    {
        var attribute =
            Attribute(IdentifierName(Global(UseProjectionAttribute)));

        return methodSyntax.AddAttributeLists(AttributeList(SingletonSeparatedList(attribute)));
    }

    public static MethodDeclarationSyntax AddGraphQLNameAttribute(
        this MethodDeclarationSyntax methodSyntax,
        string name)
    {
        var attribute =
            Attribute(IdentifierName(Global(typeof(GraphQLNameAttribute).FullName!)))
                .AddArgumentListArguments(
                    AttributeArgument(
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(name))));

        return methodSyntax.AddAttributeLists(AttributeList(SingletonSeparatedList(attribute)));
    }

#pragma warning disable CS0618
    public static ParameterSyntax AddScopedServiceAttribute(
        this ParameterSyntax methodSyntax)
    {
        var attribute =
            Attribute(IdentifierName(Global(typeof(ScopedServiceAttribute).FullName!)));

        return methodSyntax.AddAttributeLists(AttributeList(SingletonSeparatedList(attribute)));
    }
#pragma warning restore CS0618
}
