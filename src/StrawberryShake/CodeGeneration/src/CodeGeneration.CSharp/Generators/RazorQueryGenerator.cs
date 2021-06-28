using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
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
            string componentName = descriptor.Name.Value + "Renderer";
            string resultType = descriptor.ResultTypeReference.GetRuntimeType().ToString();

            ClassDeclarationSyntax classDeclaration =
                ClassDeclaration(componentName)
                    .AddImplements(TypeNames.QueryBase.WithGeneric(resultType))
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
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
            PropertyDeclarationSyntax propertySyntax =
                PropertyDeclaration(property.Type.ToTypeSyntax(), GetPropertyName(property.Name))
                    .WithAttributeLists(
                        SingletonList(
                            AttributeList(
                                SingletonSeparatedList(
                                    Attribute(
                                        IdentifierName(TypeNames.ParameterAttribute))))))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .WithGetterAndSetter();

            if (property.Type.IsNonNullable())
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

            SyntaxList<StatementSyntax> bodyStatements =
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
    }
}
