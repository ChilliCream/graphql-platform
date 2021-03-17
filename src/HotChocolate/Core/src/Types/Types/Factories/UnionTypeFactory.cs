using System;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class UnionTypeFactory
        : ITypeFactory<UnionTypeDefinitionNode, UnionType>
    {
        public UnionType Create(
            IBindingLookup bindingLookup,
            IReadOnlySchemaOptions schemaOptions,
            UnionTypeDefinitionNode node)
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

            return new UnionType(d =>
            {
                d.SyntaxNode(schemaOptions.PreserveSyntaxNodes ? node : null)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

                if (bindingInfo.SourceType != null)
                {
                    d.Extend().OnBeforeCreate(t => t.RuntimeType = bindingInfo.SourceType);
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
