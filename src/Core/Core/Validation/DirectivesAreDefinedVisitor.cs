using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class DirectivesAreDefinedVisitor
        : QueryVisitorErrorBase
    {
        private readonly HashSet<string> _directives;

        public DirectivesAreDefinedVisitor(ISchema schema)
            : base(schema)
        {
            _directives = new HashSet<string>(
                schema.DirectiveTypes.Select(t => t.Name));
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            if (!_directives.Contains(directive.Name.Value))
            {
                Errors.Add(new ValidationError(
                    $"The specified directive `{directive.Name.Value}` " +
                    "is not supported by the current schema.",
                    directive));
            }
        }
    }
}
