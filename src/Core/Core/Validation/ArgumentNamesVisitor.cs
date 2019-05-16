using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class ArgumentNamesVisitor
        : QueryVisitorErrorBase
    {
        private readonly Dictionary<NameString, DirectiveType> _directives;

        public ArgumentNamesVisitor(ISchema schema)
            : base(schema)
        {
            _directives = schema.DirectiveTypes.ToDictionary(t => t.Name);
        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is IComplexOutputType t
                && t.Fields.TryGetField(field.Name.Value, out IOutputField f))
            {
                CheckArguments(f.Arguments, field.Arguments);
            }

            base.VisitField(field, type, path);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            if (_directives.TryGetValue(directive.Name.Value,
                out DirectiveType d))
            {
                CheckArguments(d.Arguments, directive.Arguments);
            }

            base.VisitDirective(directive, path);
        }

        private void CheckArguments(
            IFieldCollection<IInputField> declaredArguments,
            IReadOnlyCollection<ArgumentNode> assignedArguments)
        {
            foreach (ArgumentNode argument in assignedArguments)
            {
                if (!declaredArguments.ContainsField(argument.Name.Value))
                {
                    Errors.Add(new ValidationError(
                        $"The argument `{argument.Name.Value}` does not " +
                        "exist.", argument));
                }
            }
        }
    }
}
