
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class InputObjectRequiredFieldsVisitor
        : InputObjectFieldVisitorBase
    {
        private readonly HashSet<ObjectValueNode> _visited =
            new HashSet<ObjectValueNode>();

        public InputObjectRequiredFieldsVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitObjectValue(
            InputObjectType type,
            ObjectValueNode objectValue)
        {
            if (_visited.Add(objectValue))
            {
                Dictionary<string, ObjectFieldNode> fieldValues =
                    CreateFieldMap(objectValue);

                foreach (InputField field in type.Fields)
                {
                    fieldValues.TryGetValue(field.Name,
                        out ObjectFieldNode fieldValue);

                    ValidateInputField(field, fieldValue,
                        (ISyntaxNode)fieldValue ?? objectValue);

                    if (fieldValue?.Value is ObjectValueNode ov
                        && field.Type.NamedType() is InputObjectType it)
                    {
                        VisitObjectValue(it, ov);
                    }
                }
            }
        }

        private void ValidateInputField(
            InputField field,
            ObjectFieldNode fieldValue,
            ISyntaxNode node)
        {
            if (field.Type.IsNonNullType()
                && field.DefaultValue.IsNull()
                && ValueNodeExtensions.IsNull(fieldValue?.Value))
            {
                Errors.Add(new ValidationError(
                    $"`{field.Name}` is a required field and cannot be null.",
                    node));
            }
        }

        private Dictionary<string, ObjectFieldNode> CreateFieldMap(
            ObjectValueNode objectValue)
        {
            var fields = new Dictionary<string, ObjectFieldNode>();

            foreach (ObjectFieldNode fieldValue in objectValue.Fields)
            {
                if (!fields.ContainsKey(fieldValue.Name.Value))
                {
                    fields[fieldValue.Name.Value] = fieldValue;
                }
            }

            return fields;
        }
    }
}
