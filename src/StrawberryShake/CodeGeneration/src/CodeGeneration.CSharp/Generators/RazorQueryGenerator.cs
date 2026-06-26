using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class RazorQueryGenerator : CSharpSyntaxGenerator<OperationDescriptor>
{
    protected override bool CanHandle(
        OperationDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings) =>
        settings.RazorComponents && descriptor is QueryOperationDescriptor;

    protected override CSharpSyntaxGeneratorResult Generate(
        OperationDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings)
    {
        var componentName = $"Use{descriptor.Name}";
        var resultType = descriptor.ResultTypeReference.GetRuntimeType().ToString();

        var modifier = settings.AccessModifier == AccessModifier.Public
            ? SyntaxKind.PublicKeyword
            : SyntaxKind.InternalKeyword;

        // Persisted component state requires a store to rehydrate into.
        var persistedState = settings.RazorPersistedState && !settings.NoStore;

        var baseType = persistedState
            ? TypeNames.UsePersistentQuery.WithGeneric(resultType)
            : TypeNames.UseQuery.WithGeneric(resultType);

        var classDeclaration =
            ClassDeclaration(componentName)
                .AddImplements(baseType)
                .AddModifiers(
                    Token(modifier),
                    Token(SyntaxKind.PartialKeyword))
                .AddGeneratedAttribute()
                .AddMembers(CreateOperationProperty(descriptor.RuntimeType.ToString()));

        foreach (var argument in descriptor.Arguments)
        {
            classDeclaration = classDeclaration.AddMembers(
                CreateArgumentProperty(argument));
        }

        if (persistedState)
        {
            classDeclaration = classDeclaration.AddMembers(
                CreatePersistenceKeyMethod(descriptor.Arguments));
            classDeclaration = classDeclaration.AddMembers(
                CreateWatchMethod(resultType, descriptor.Arguments));
        }
        else
        {
            classDeclaration = classDeclaration.AddMembers(
                CreateLifecycleMethodMethod("OnInitialized", descriptor.Arguments));
            classDeclaration = classDeclaration.AddMembers(
                CreateLifecycleMethodMethod("OnParametersSet", descriptor.Arguments));
        }

        return new CSharpSyntaxGeneratorResult(
            componentName,
            Components,
            $"{descriptor.RuntimeType.NamespaceWithoutGlobal}.{Components}",
            classDeclaration,
            isRazorComponent: true);
    }

    private PropertyDeclarationSyntax CreateOperationProperty(string typeName) =>
        PropertyDeclaration(ParseTypeName(typeName), "Operation")
            .WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                IdentifierName(TypeNames.InjectAttribute))))))
            .AddModifiers(Token(SyntaxKind.InternalKeyword))
            .WithGetterAndSetter()
            .WithSuppressNullableWarningExpression();

    private PropertyDeclarationSyntax CreateArgumentProperty(PropertyDescriptor property)
    {
        var propertySyntax =
            PropertyDeclaration(property.Type.ToTypeSyntax(), GetPropertyName(property.Name))
                .WithAttributeLists(
                    SingletonList(
                        AttributeList(
                            SingletonSeparatedList(
                                Attribute(
                                    IdentifierName(TypeNames.ParameterAttribute))))))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithGetterAndSetter();

        if (property.Type.IsNonNull())
        {
            propertySyntax = propertySyntax.WithSuppressNullableWarningExpression();
        }

        return propertySyntax;
    }

    private MethodDeclarationSyntax CreateLifecycleMethodMethod(
        string methodName,
        IReadOnlyList<PropertyDescriptor> arguments)
    {
        var argumentList = new List<ArgumentSyntax>();

        foreach (var argument in arguments)
        {
            argumentList.Add(Argument(IdentifierName(GetPropertyName(argument.Name))));
        }

        argumentList.Add(
            Argument(IdentifierName("Strategy"))
                .WithNameColon(NameColon(IdentifierName("strategy"))));

        var bodyStatements =
            SingletonList<StatementSyntax>(
                ExpressionStatement(
                    InvocationExpression(
                            IdentifierName("Subscribe"))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("Operation"),
                                                    IdentifierName("Watch")))
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SeparatedList(argumentList)))))))));

        return MethodDeclaration(
                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier(methodName))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.ProtectedKeyword),
                    Token(SyntaxKind.OverrideKeyword)))
            .WithBody(Block(bodyStatements));
    }

    private MethodDeclarationSyntax CreatePersistenceKeyMethod(
        IReadOnlyList<PropertyDescriptor> arguments)
    {
        var argumentList = new List<ArgumentSyntax>();

        foreach (var argument in arguments)
        {
            argumentList.Add(Argument(IdentifierName(GetPropertyName(argument.Name))));
        }

        var bodyStatements =
            SingletonList<StatementSyntax>(
                ReturnStatement(
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("Operation"),
                                IdentifierName("GetPersistenceKey")))
                        .WithArgumentList(
                            ArgumentList(SeparatedList(argumentList)))));

        return MethodDeclaration(
                ParseTypeName(TypeNames.String),
                Identifier("GetPersistenceKey"))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.ProtectedKeyword),
                    Token(SyntaxKind.OverrideKeyword)))
            .WithBody(Block(bodyStatements));
    }

    private MethodDeclarationSyntax CreateWatchMethod(
        string resultType,
        IReadOnlyList<PropertyDescriptor> arguments)
    {
        var argumentList = new List<ArgumentSyntax>();

        foreach (var argument in arguments)
        {
            argumentList.Add(Argument(IdentifierName(GetPropertyName(argument.Name))));
        }

        argumentList.Add(Argument(IdentifierName("persistedState")));

        var bodyStatements =
            SingletonList<StatementSyntax>(
                ReturnStatement(
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("Operation"),
                                IdentifierName("Watch")))
                        .WithArgumentList(
                            ArgumentList(SeparatedList(argumentList)))));

        var returnType =
            ParseTypeName(
                "global::System.IObservable<"
                + TypeNames.IOperationResult.WithGeneric(resultType)
                + ">");

        return MethodDeclaration(returnType, Identifier("CreateWatch"))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.ProtectedKeyword),
                    Token(SyntaxKind.OverrideKeyword)))
            .AddParameterListParameters(
                Parameter(Identifier("persistedState"))
                    .WithType(
                        ParseTypeName("global::System.ReadOnlyMemory<global::System.Byte>?")))
            .WithBody(Block(bodyStatements));
    }
}
