using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class DirectiveDescription
    {
        public DirectiveDescription(DirectiveNode parsedDirective)
        {
            ParsedDirective = parsedDirective
                ?? throw new ArgumentNullException(nameof(parsedDirective));
        }

        public DirectiveDescription(object customDirective)
        {
            CustomDirective = customDirective
                ?? throw new ArgumentNullException(nameof(customDirective));
        }

        public DirectiveNode ParsedDirective { get; }

        public object CustomDirective { get; }
    }
}
