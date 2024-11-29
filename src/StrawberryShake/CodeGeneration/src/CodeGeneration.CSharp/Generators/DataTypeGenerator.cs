using Microsoft.CodeAnalysis.CSharp;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class DataTypeGenerator : CSharpSyntaxGenerator<DataTypeDescriptor>
{
    protected override CSharpSyntaxGeneratorResult Generate(
        DataTypeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings)
    {
        var modifier = settings.AccessModifier == AccessModifier.Public
            ? SyntaxKind.PublicKeyword
            : SyntaxKind.InternalKeyword;

        return descriptor.IsInterface
            ? GenerateDataInterface(descriptor, modifier)
            : GenerateDataClass(descriptor, modifier, settings.EntityRecords);
    }

    private CSharpSyntaxGeneratorResult GenerateDataInterface(
        DataTypeDescriptor descriptor,
        SyntaxKind accessModifier)
    {
        var interfaceDeclaration =
            InterfaceDeclaration(descriptor.RuntimeType.Name)
                .AddModifiers(
                    Token(accessModifier),
                    Token(SyntaxKind.PartialKeyword))
                .AddGeneratedAttribute()
                .AddSummary(descriptor.Documentation)
                .AddImplements(descriptor.Implements.Select(CreateDataTypeName).ToArray());

        interfaceDeclaration = interfaceDeclaration.AddTypeProperty();

        ForEachProperty(
            descriptor,
            p => interfaceDeclaration = interfaceDeclaration.AddStateProperty(p));

        return new(
            descriptor.RuntimeType.Name,
            State,
            descriptor.RuntimeType.NamespaceWithoutGlobal,
            interfaceDeclaration);
    }

    private CSharpSyntaxGeneratorResult GenerateDataClass(
        DataTypeDescriptor descriptor,
        SyntaxKind accessModifier,
        bool hasEntityRecords)
    {
        if (hasEntityRecords)
        {
            var recordDeclarationSyntax =
                RecordDeclaration(Token(SyntaxKind.RecordKeyword), descriptor.RuntimeType.Name)
                    .AddModifiers(
                        Token(accessModifier),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute()
                    .AddSummary(descriptor.Documentation)
                    .AddImplements(descriptor.Implements.Select(CreateDataTypeName).ToArray())
                    .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken));

            // Adds the constructor
            var constructor =
                ConstructorDeclaration(descriptor.RuntimeType.Name)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword));

            constructor = constructor.AddTypeParameter();

            ForEachProperty(
                descriptor,
                p => constructor = constructor.AddStateParameter(p));

            recordDeclarationSyntax = recordDeclarationSyntax.AddMembers(constructor);

            // Adds the property
            recordDeclarationSyntax = recordDeclarationSyntax.AddTypeProperty();

            ForEachProperty(
                descriptor,
                p => recordDeclarationSyntax = recordDeclarationSyntax.AddStateProperty(p));

            recordDeclarationSyntax = recordDeclarationSyntax.WithCloseBraceToken(
                Token(SyntaxKind.CloseBraceToken));

            return new(
                descriptor.RuntimeType.Name,
                State,
                descriptor.RuntimeType.NamespaceWithoutGlobal,
                recordDeclarationSyntax);
        }
        else
        {
            var classDeclaration =
                ClassDeclaration(descriptor.RuntimeType.Name)
                    .AddModifiers(
                        Token(accessModifier),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute()
                    .AddSummary(descriptor.Documentation)
                    .AddImplements(descriptor.Implements.Select(CreateDataTypeName).ToArray());

            // Adds the constructor
            var constructor =
                ConstructorDeclaration(descriptor.RuntimeType.Name)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword));

            constructor = constructor.AddTypeParameter();

            ForEachProperty(
                descriptor,
                p => constructor = constructor.AddStateParameter(p));

            classDeclaration = classDeclaration.AddMembers(constructor);

            // Adds the property
            classDeclaration = classDeclaration.AddTypeProperty();

            ForEachProperty(
                descriptor,
                p => classDeclaration = classDeclaration.AddStateProperty(p));

            return new(
                descriptor.RuntimeType.Name,
                State,
                descriptor.RuntimeType.NamespaceWithoutGlobal,
                classDeclaration);
        }
    }

    public void ForEachProperty(
        DataTypeDescriptor descriptor,
        Action<PropertyDescriptor> action)
    {
        foreach (var property in descriptor.Properties)
        {
            if (property.Name.EqualsOrdinal(WellKnownNames.TypeName))
            {
                continue;
            }

            action(property);
        }
    }
}
