using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class DirectivesAreUniquePerLocationVisitor
        : QueryVisitorErrorBase
    {
        private readonly HashSet<string> _directives = new HashSet<string>();

        public DirectivesAreUniquePerLocationVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitDirectives(
            IReadOnlyCollection<DirectiveNode> directives,
            ImmutableStack<ISyntaxNode> path)
        {
            _directives.Clear();

            foreach (DirectiveNode directive in directives)
            {
                if (!_directives.Add(directive.Name.Value))
                {
                    Errors.Add(new ValidationError(
                        "Only one of each directive is allowed per location.",
                        directive));
                }
            }
        }
    }
}
