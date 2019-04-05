using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public sealed class DirectiveDefinition
    {
        public DirectiveDefinition(DirectiveNode parsedDirective)
        {
            ParsedDirective = parsedDirective
                ?? throw new ArgumentNullException(nameof(parsedDirective));
            Reference = new NameDirectiveReference(parsedDirective.Name.Value);
            TypeReference = new SyntaxTypeReference(
                new NamedTypeNode(parsedDirective.Name),
                TypeContext.None);
        }

        public DirectiveDefinition(object customDirective)
        {
            CustomDirective = customDirective
                ?? throw new ArgumentNullException(nameof(customDirective));
            Reference = new ClrTypeDirectiveReference(
                customDirective.GetType());
            TypeReference = new ClrTypeReference(
                customDirective.GetType(),
                TypeContext.None);
        }

        public DirectiveNode ParsedDirective { get; }

        public object CustomDirective { get; }

        public IDirectiveReference Reference { get; }

        public ITypeReference TypeReference { get; }
    }
}
