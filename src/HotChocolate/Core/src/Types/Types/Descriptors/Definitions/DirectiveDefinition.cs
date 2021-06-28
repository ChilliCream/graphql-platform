using System;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public sealed class DirectiveDefinition
    {
        public DirectiveDefinition(DirectiveNode parsedDirective)
        {
            ParsedDirective = parsedDirective ??
                throw new ArgumentNullException(nameof(parsedDirective));
            TypeReference =
                Descriptors.TypeReference.Create(parsedDirective.Name.Value, TypeContext.None);
            Reference = new NameDirectiveReference(parsedDirective.Name.Value);
        }

        public DirectiveDefinition(object customDirective, ITypeReference typeReference)
        {
            CustomDirective = customDirective ??
                throw new ArgumentNullException(nameof(customDirective));
            TypeReference = typeReference ??
                throw new ArgumentNullException(nameof(typeReference));
            Reference = new ClrTypeDirectiveReference(customDirective.GetType());
        }

        public DirectiveNode? ParsedDirective { get; }

        public object? CustomDirective { get; }

        public IDirectiveReference Reference { get; }

        public ITypeReference TypeReference { get; }
    }
}
