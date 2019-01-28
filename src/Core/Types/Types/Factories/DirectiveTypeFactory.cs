using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class DirectiveTypeFactory
        : ITypeFactory<DirectiveDefinitionNode, DirectiveType>
    {
        public DirectiveType Create(DirectiveDefinitionNode node)
        {
            return new DirectiveType(c =>
            {
                c.Name(node.Name.Value);
                c.Description(node.Description?.Value);
                c.SyntaxNode(node);

                if (node.IsRepeatable)
                {
                    c.Repeatable();
                }

                DeclareArguments(c, node);
                DeclareLocations(c, node);
            });
        }

        private static void DeclareArguments(
            IDirectiveTypeDescriptor typeDescriptor,
            DirectiveDefinitionNode node)
        {
            foreach (InputValueDefinitionNode inputField in node.Arguments)
            {
                IArgumentDescriptor descriptor = typeDescriptor
                    .Argument(inputField.Name.Value)
                    .Description(inputField.Description?.Value)
                    .Type(inputField.Type)
                    .DefaultValue(inputField.DefaultValue)
                    .SyntaxNode(inputField);

                foreach (DirectiveNode directive in inputField.Directives)
                {
                    descriptor.Directive(directive);
                }
            }
        }

        private static void DeclareLocations(
            IDirectiveTypeDescriptor typeDescriptor,
            DirectiveDefinitionNode node)
        {
            foreach (NameNode location in node.Locations)
            {
                if (Enum.TryParse(location.Value, true,
                    out DirectiveLocation parsedLocation))
                {
                    typeDescriptor.Location(parsedLocation);
                }
            }
        }
    }
}
