
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class InputObjectFieldUniquenessVisitor
        : InputObjectFieldVisitorBase
    {
        private readonly HashSet<ObjectValueNode> _visited =
            new HashSet<ObjectValueNode>();

        public InputObjectFieldUniquenessVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitObjectValue(
            InputObjectType type,
            ObjectValueNode objectValue)
        {
            var visitedFields = new HashSet<string>();

            if (_visited.Add(objectValue))
            {
                foreach (ObjectFieldNode fieldValue in objectValue.Fields)
                {
                    if (type.Fields.TryGetField(fieldValue.Name.Value,
                        out InputField field))
                    {
                        VisitInputField(visitedFields, field, fieldValue);
                    }
                }
            }
        }

        private void VisitInputField(
            HashSet<string> visitedFields,
            InputField field,
            ObjectFieldNode fieldValue)
        {
            if (visitedFields.Add(field.Name))
            {
                if (fieldValue.Value is ObjectValueNode ov
                    && field.Type.NamedType() is InputObjectType it)
                {
                    VisitObjectValue(it, ov);
                }
            }
            else
            {
                Errors.Add(new ValidationError(
                    $"Field `{field.Name}` is ambiguous.",
                    fieldValue));
            }
        }
    }
}
