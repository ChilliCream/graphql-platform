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
            IReadOnlySchemaOptions schemaOptions,
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
                d.SyntaxNode(schemaOptions.PreserveSyntaxNodes ? node : null)
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
                    typeDescriptor
                        .Value(value.Name.Value)
                        .Description(value.Description?.Value)
                        .Name(value.Name.Value);

                if (value.DeprecationReason() is { Length: > 0 } s)
                {
                    valueDescriptor.Deprecated(s);
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
