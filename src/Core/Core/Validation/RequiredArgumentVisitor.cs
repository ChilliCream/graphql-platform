using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class RequiredArgumentVisitor
        : QueryVisitorErrorBase
    {
        private readonly Dictionary<string, DirectiveType> _directives;

        public RequiredArgumentVisitor(ISchema schema)
            : base(schema)
        {
            _directives = schema.DirectiveTypes.ToDictionary(t => t.Name);
        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is IComplexOutputType complexType
                && complexType.Fields.ContainsField(field.Name.Value))
            {
                ValidateRequiredArguments(field, field.Arguments,
                    complexType.Fields[field.Name.Value].Arguments);
            }


            base.VisitField(field, type, path);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            if (_directives.TryGetValue(directive.Name.Value, out DirectiveType d))
            {
                ValidateRequiredArguments(
                    directive, directive.Arguments,
                    d.Arguments);
            }

            base.VisitDirective(directive, path);
        }

        private void ValidateRequiredArguments(
            ISyntaxNode parent,
            IEnumerable<ArgumentNode> providedArguments,
            IFieldCollection<IInputField> arguments)
        {
            ILookup<string, ArgumentNode> providedArgumentLookup =
                providedArguments.ToLookup(t => t.Name.Value);

            foreach (IInputField requiredArgument in arguments
                .Where(t => IsRequiredArgument(t)))
            {
                ArgumentNode providedArgument =
                    providedArgumentLookup[requiredArgument.Name]
                        .FirstOrDefault();

                if (providedArgument == null
                    || providedArgument.Value is NullValueNode)
                {
                    Errors.Add(new ValidationError(
                        $"The argument `{requiredArgument.Name}` is required " +
                        "and does not allow null values.", parent));
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
