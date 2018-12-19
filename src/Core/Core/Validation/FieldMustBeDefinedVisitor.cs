
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

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
                && !FieldExists(ct, field.Name.Value))
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
                .All(t => IsTypeNameField(t.Name.Value));
        }

        private bool FieldExists(IComplexOutputType type, NameString fieldName)
        {
            if (IsTypeNameField(fieldName))
            {
                return true;
            }
            return type.Fields.ContainsField(fieldName);
        }

        private static bool IsTypeNameField(NameString fieldName)
        {
            return fieldName.Equals(IntrospectionFields.TypeName);
        }
    }
}
