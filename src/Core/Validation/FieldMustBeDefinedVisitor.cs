
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class FieldMustBeDefinedVisitor
        : QueryVisitorErrorBase
    {
        public FieldMustBeDefinedVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitSelectionSet(
            SelectionSetNode selectionSet,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is UnionType ut
                && !HasOnylTypeNameField(selectionSet))
            {
                Errors.Add(new ValidationError(
                    "A union type cannot declare a field directly. " +
                    "Use inline fragments or fragments instead", selectionSet));
            }
            else
            {
                base.VisitSelectionSet(selectionSet, type, path);
            }
        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is IComplexOutputType ct
                && !ct.Fields.ContainsField(field.Name.Value))
            {
                Errors.Add(new ValidationError(
                    $"The field `{field.Name.Value}` does not exist " +
                    $"on the type `{ct.Name}`.", field));
            }

            base.VisitField(field, type, path);
        }

        private bool HasOnylTypeNameField(SelectionSetNode selectionSet)
        {
            return selectionSet.Selections
                .OfType<FieldNode>()
                .All(t => t.Name.Value.EqualsOrdinal("__typename"));
        }
    }
}
