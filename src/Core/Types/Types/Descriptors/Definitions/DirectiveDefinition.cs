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
        }

        public DirectiveDefinition(object customDirective)
        {
            CustomDirective = customDirective
                ?? throw new ArgumentNullException(nameof(customDirective));
        }

        public DirectiveNode ParsedDirective { get; }

        public object CustomDirective { get; }
    }
}
