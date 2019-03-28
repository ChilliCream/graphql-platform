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
        }

        public DirectiveDefinition(object customDirective)
        {
            CustomDirective = customDirective
                ?? throw new ArgumentNullException(nameof(customDirective));
            Reference = new ClrTypeDirectiveReference(
                customDirective.GetType());
        }

        public DirectiveNode ParsedDirective { get; }

        public object CustomDirective { get; }

        public IDirectiveReference Reference { get; }

    }
}
