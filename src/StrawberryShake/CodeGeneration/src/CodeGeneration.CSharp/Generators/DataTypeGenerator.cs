using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class DataTypeGenerator : CSharpSyntaxGenerator<DataTypeDescriptor>
    {
        protected override CSharpSyntaxGeneratorResult Generate(
            DataTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings)
        {
            if (descriptor.IsInterface)
            {
                return GenerateDataInterface(descriptor);
            }

            return GenerateDataClass(descriptor, settings);
        }

        private CSharpSyntaxGeneratorResult GenerateDataInterface(
            DataTypeDescriptor descriptor)
        {
            InterfaceDeclarationSyntax interfaceDeclaration =
                InterfaceDeclaration(descriptor.RuntimeType.Name)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute()
                    .AddSummary(descriptor.Documentation)
                    .AddImplements(descriptor.Implements);

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
            CSharpSyntaxGeneratorSettings settings)
        {
            if (settings.EntityRecords)
            {
                RecordDeclarationSyntax recordDeclarationSyntax =
                    RecordDeclaration(Token(SyntaxKind.RecordKeyword), descriptor.RuntimeType.Name)
                        .AddModifiers(
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.PartialKeyword))
                        .AddGeneratedAttribute()
                        .AddSummary(descriptor.Documentation)
                        .AddImplements(descriptor.Implements)
                        .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken));

                // Adds the constructor
                ConstructorDeclarationSyntax constructor =
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
                ClassDeclarationSyntax classDeclaration =
                    ClassDeclaration(descriptor.RuntimeType.Name)
                        .AddModifiers(
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.PartialKeyword))
                        .AddGeneratedAttribute()
                        .AddSummary(descriptor.Documentation)
                        .AddImplements(descriptor.Implements);

                // Adds the constructor
                ConstructorDeclarationSyntax constructor =
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
            foreach (PropertyDescriptor property in descriptor.Properties)
            {
                if (property.Name.Value.EqualsOrdinal(WellKnownNames.TypeName))
                {
                    continue;
                }

                action(property);
            }
        }
    }
}
