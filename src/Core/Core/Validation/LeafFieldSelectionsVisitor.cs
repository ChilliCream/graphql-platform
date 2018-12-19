using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class LeafFieldSelectionsVisitor
        : QueryVisitorErrorBase
    {
        public LeafFieldSelectionsVisitor(ISchema schema)
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
                if (f.Type.NamedType().IsScalarType()
                    || f.Type.NamedType().IsEnumType())
                {
                    ValidateLeafField(field, f);
                }
                else
                {
                    ValidateNodeField(field, f);
                    base.VisitField(field, type, path);
                }
            }
        }

        private void ValidateLeafField(
            FieldNode fieldSelection,
            IOutputField field)
        {
            if (fieldSelection.SelectionSet != null)
            {
                string type = field.Type.IsScalarType() ? "a scalar" : "an enum";
                Errors.Add(new ValidationError(
                    $"`{field.Name}` is {type} field. Selections on scalars " +
                    "or enums are never allowed, because they are the leaf " +
                    "nodes of any GraphQL query.", fieldSelection));
            }
        }

        private void ValidateNodeField(
            FieldNode fieldSelection,
            IOutputField field)
        {
            if (fieldSelection.SelectionSet == null)
            {
                Errors.Add(new ValidationError(
                    $"`{field.Name}` is an object, interface or union type " +
                    "field. Leaf selections on objects, interfaces, and " +
                    "unions without subfields are disallowed.",
                    fieldSelection));
            }
        }
    }
}
