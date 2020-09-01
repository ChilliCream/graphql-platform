using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class EnumTypeFactory
        : ITypeFactory<EnumTypeDefinitionNode, EnumType>
    {
        public EnumType Create(
            IBindingLookup bindingLookup,
            EnumTypeDefinitionNode node)
        {
            if (bindingLookup is null)
            {
                throw new ArgumentNullException(nameof(bindingLookup));
            }

            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            ITypeBindingInfo bindingInfo =
                bindingLookup.GetBindingInfo(node.Name.Value);

            return new EnumType(d =>
            {
                d.SyntaxNode(node)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

                if (bindingInfo.SourceType != null)
                {
                    d.Extend().OnBeforeCreate(
                        t => t.RuntimeType = bindingInfo.SourceType);
                }

                foreach (DirectiveNode directive in node.Directives)
                {
                    if (!directive.IsDeprecationReason())
                    {
                        d.Directive(directive);
                    }
                }

                DeclareValues(d, node.Values);
            });
        }

        private static void DeclareValues(
            IEnumTypeDescriptor typeDescriptor,
            IReadOnlyCollection<EnumValueDefinitionNode> values)
        {
            foreach (EnumValueDefinitionNode value in values)
            {
                IEnumValueDescriptor valueDescriptor =
                    typeDescriptor.Value(value.Name.Value)
                        .Description(value.Description?.Value);

                string deprecactionReason = value.DeprecationReason();
                if (!string.IsNullOrEmpty(deprecactionReason))
                {
                    valueDescriptor.Deprecated(deprecactionReason);
                }

                foreach (DirectiveNode directive in value.Directives)
                {
                    if (!directive.IsDeprecationReason())
                    {
                        valueDescriptor.Directive(directive);
                    }
                }
            }
        }
    }
}
