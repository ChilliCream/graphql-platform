using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class ArgumentNamesRule
    {

    }

    internal sealed class ArgumentNamesVisitor
        : QueryVisitorErrorBase
    {
        public ArgumentNamesVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is IComplexOutputType t
                && t.Fields.TryGetField(field.Name.Value, out IOutputField f))
            {
                foreach (ArgumentNode argument in field.Arguments)
                {
                    if (!f.Arguments.ContainsField(argument.Name.Value))
                    {
                        Errors.Add(new ValidationError(
                            $"The argument `{argument.Name.Value}` does not " +
                            "exist.", argument));
                    }
                }
            }

            base.VisitField(field, type, path);
        }
    }
}
