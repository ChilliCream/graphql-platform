using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal abstract class InputObjectFieldVisitorBase
        : QueryVisitorErrorBase
    {
        private readonly Dictionary<string, DirectiveType> _directives;

        protected InputObjectFieldVisitorBase(ISchema schema)
            : base(schema)
        {
            _directives = schema.DirectiveTypes.ToDictionary(t => t.Name);
        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is IComplexOutputType ct
                && ct.Fields.TryGetField(field.Name.Value, out IOutputField f))
            {
                VisitArguments(field.Arguments, f.Arguments);
            }
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            if (_directives.TryGetValue(directive.Name.Value, out DirectiveType d))
            {
                VisitArguments(directive.Arguments, d.Arguments);
            }
        }

        private void VisitArguments(
            IEnumerable<ArgumentNode> arguments,
            IFieldCollection<IInputField> argumentFields)
        {
            foreach (ArgumentNode argument in arguments)
            {
                if (argument.Value is ObjectValueNode ov
                    && argumentFields.TryGetField(argument.Name.Value,
                        out IInputField argumentField)
                    && argumentField.Type.NamedType() is InputObjectType io)
                {
                    VisitObjectValue(io, ov);
                }
            }
        }

        protected abstract void VisitObjectValue(
            InputObjectType type,
            ObjectValueNode objectValue);
    }
}
