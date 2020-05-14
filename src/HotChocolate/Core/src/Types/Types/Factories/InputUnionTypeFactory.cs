using System;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class InputUnionTypeFactory
        : ITypeFactory<InputUnionTypeDefinitionNode, InputUnionType>
    {
        public InputUnionType Create(
            IBindingLookup bindingLookup,
            InputUnionTypeDefinitionNode node)
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

            return new InputUnionType(d =>
            {
                d.SyntaxNode(node)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

                if (bindingInfo.SourceType != null)
                {
                    d.Extend().OnBeforeCreate(
                        t => t.ClrType = bindingInfo.SourceType);
                }

                foreach (NamedTypeNode namedType in node.Types)
                {
                    d.Type(namedType);
                }

                foreach (DirectiveNode directive in node.Directives)
                {
                    d.Directive(directive);
                }
            });
        }
    }
}
