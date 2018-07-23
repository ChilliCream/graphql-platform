using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class DirectivesAreUniquePerLocationVisitor
        : QueryVisitor
    {
        private readonly HashSet<string> _directives = new HashSet<string>();
        private readonly List<ValidationError> _errors =
            new List<ValidationError>();

        public DirectivesAreUniquePerLocationVisitor(ISchema schema)
            : base(schema)
        {
        }

        public IReadOnlyCollection<ValidationError> Errors => _errors;

        protected override void VisitDirectives(
            IReadOnlyCollection<DirectiveNode> directives,
            ImmutableStack<ISyntaxNode> path)
        {
            _directives.Clear();

            ISyntaxNode node = path.Peek();

            foreach (DirectiveNode directive in directives)
            {
                if (!_directives.Add(directive.Name.Value))
                {
                    _errors.Add(new ValidationError(
                        "Only one of each directive is allowed per location.",
                        directive));
                }
            }
        }
    }
}
