using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class RequiredArgumentVisitor
        : QueryVisitor
    {
        private readonly List<ValidationError> _errors =
            new List<ValidationError>();

        public RequiredArgumentVisitor(ISchema schema)
            : base(schema)
        {
        }

        public IReadOnlyCollection<ValidationError> Errors => _errors;

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is IComplexOutputType complexType)
            {
                if (complexType.Fields.ContainsField(field.Name.Value))
                {
                    ValidateRequiredArguments(field.Arguments,
                        complexType.Fields[field.Name.Value].Arguments);
                }
            }

            base.VisitField(field, type, path);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            base.VisitDirective(directive, path);
        }

        private void ValidateRequiredArguments(
            IEnumerable<ArgumentNode> providedArguments,
            IFieldCollection<IInputField> arguments)
        {
            ILookup<string, ArgumentNode> providedArgumentLookup =
                providedArguments.ToLookup(t => t.Name.Value);

            foreach (InputField requiredArgument in arguments
                .Where(t => IsRequiredArgument(t)))
            {
                ArgumentNode providedArgument =
                    providedArgumentLookup[requiredArgument.Name]
                        .FirstOrDefault();

                if (providedArgument == null
                    || providedArgument.Value is NullValueNode)
                {
                    _errors.Add(new ValidationError(
                        $"The argument `{requiredArgument.Name}` is required " +
                        "and does not allow null values.", providedArgument));
                }
            }
        }

        private static bool IsRequiredArgument(IInputField argument)
        {
            return argument.Type.IsNonNullType()
                && argument.DefaultValue is NullValueNode;
        }
    }
}
