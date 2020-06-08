using System;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// A scalar called _FieldSet is a custom scalar type that is used to 
    /// represent a set of fields.
    /// 
    /// Grammatically, a field set is a selection set minus the braces.
    /// 
    /// This means it can represent a single field "upc", multiple fields "id countryCode",
    /// and even nested selection sets "id organization { id }".
    /// </summary>
    public class FieldSetType
        : ScalarType<SelectionSetNode, StringValueNode>
    {
        public FieldSetType()
            : base(TypeNames.FieldSet, BindingBehavior.Explicit)
        {
            Description = FederationResources.FieldsetType_Description;
        }

        public FieldSetType(NameString name, BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = FederationResources.FieldsetType_Description;
        }

        protected override SelectionSetNode ParseLiteral(StringValueNode literal)
        {
            // parse selection set
            throw new NotImplementedException();
        }

        protected override StringValueNode ParseValue(SelectionSetNode value)
        {
            // serialize selection set
            throw new NotImplementedException();
        }

        public override bool TrySerialize(object? value, out object? serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is SelectionSetNode selectionSet)
            {
                // serialize selection set
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object? serialized, out object? value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is SelectionSetNode selectionSet)
            {
                value = selectionSet;
                return true;
            }

            if (serialized is string serializedSelectionSet)
            {
                // parse selection set
            }

            value = null;
            return false;
        }
    }
}
