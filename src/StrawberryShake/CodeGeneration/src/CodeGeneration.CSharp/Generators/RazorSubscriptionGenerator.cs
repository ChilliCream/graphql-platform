using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class RazorSubscriptionGenerator : CSharpSyntaxGenerator<OperationDescriptor>
{
    protected override bool CanHandle(
        OperationDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings) =>
        settings.RazorComponents && descriptor is SubscriptionOperationDescriptor;

    protected override CSharpSyntaxGeneratorResult Generate(
        OperationDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings)
    {
        var componentName = $"Use{descriptor.Name}";
        var resultType = descriptor.ResultTypeReference.GetRuntimeType().ToString();

        var modifier = settings.AccessModifier == AccessModifier.Public
            ? SyntaxKind.PublicKeyword
            : SyntaxKind.InternalKeyword;

        var classDeclaration =
            ClassDeclaration(componentName)
                .AddImplements(TypeNames.UseSubscription.WithGeneric(resultType))
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

        classDeclaration = classDeclaration.AddMembers(
            CreateLifecycleMethodMethod("OnInitialized", descriptor.Arguments));
        classDeclaration = classDeclaration.AddMembers(
            CreateLifecycleMethodMethod("OnParametersSet", descriptor.Arguments));

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

        var invocation = InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("Operation"),
                IdentifierName("Watch")));

        if (argumentList.Count > 0)
        {
            invocation = invocation.WithArgumentList(ArgumentList(SeparatedList(argumentList)));
        }

        var bodyStatements =
            SingletonList<StatementSyntax>(
                ExpressionStatement(
                    InvocationExpression(
                            IdentifierName("Subscribe"))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(invocation))))));

        return MethodDeclaration(
                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier(methodName))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.ProtectedKeyword),
                    Token(SyntaxKind.OverrideKeyword)))
            .WithBody(Block(bodyStatements));
    }
}
