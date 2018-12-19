using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class ArgumentUniquenessVisitor
        : QueryVisitor
    {
        private readonly List<ISyntaxNode> _violatingNodes =
            new List<ISyntaxNode>();
        private readonly HashSet<string> _names =
            new HashSet<string>();

        public ArgumentUniquenessVisitor(ISchema schema)
            : base(schema)
        {
        }

        public IReadOnlyCollection<ISyntaxNode> ViolatingNodes => _violatingNodes;

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            ValidateArguments(field, field.Arguments);
            base.VisitField(field, type, path);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            ValidateArguments(directive, directive.Arguments);
            base.VisitDirective(directive, path);
        }

        private void ValidateArguments(ISyntaxNode node, IEnumerable<ArgumentNode> arguments)
        {
            foreach (ArgumentNode argument in arguments)
            {
                if (_names.Contains(argument.Name.Value))
                {
                    _violatingNodes.Add(node);
                    break;
                }
                _names.Add(argument.Name.Value);
            }
            _names.Clear();
        }
    }
}
