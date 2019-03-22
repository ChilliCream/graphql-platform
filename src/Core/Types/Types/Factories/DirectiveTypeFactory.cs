using System;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class DirectiveTypeFactory
        : ITypeFactory<DirectiveDefinitionNode, DirectiveType>
    {
        public DirectiveType Create(
            IBindingLookup bindingLookup,
            DirectiveDefinitionNode node)
        {
            if (bindingLookup == null)
            {
                throw new ArgumentNullException(nameof(bindingLookup));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            ITypeBindingInfo bindingInfo =
                bindingLookup.GetBindingInfo(node.Name.Value);

            return new DirectiveType(c =>
            {
                c.Name(node.Name.Value);
                c.Description(node.Description?.Value);
                c.SyntaxNode(node);

                if (bindingInfo.SourceType != null)
                {
                    c.Configure(t => t.ClrType = bindingInfo.SourceType);
                }

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
                IDirectiveArgumentDescriptor descriptor = typeDescriptor
                    .Argument(inputField.Name.Value)
                    .Description(inputField.Description?.Value)
                    .Type(inputField.Type)
                    .DefaultValue(inputField.DefaultValue)
                    .SyntaxNode(inputField);
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
