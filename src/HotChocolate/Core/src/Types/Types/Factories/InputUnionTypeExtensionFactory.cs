using System;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class InputUnionTypeExtensionFactory
        : ITypeFactory<InputUnionTypeExtensionNode, InputUnionTypeExtension>
    {
        public InputUnionTypeExtension Create(
            IBindingLookup bindingLookup,
            InputUnionTypeExtensionNode node)
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

            return new InputUnionTypeExtension(d =>
            {
                d.Name(node.Name.Value);

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
