using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class InputObjectFieldNamesVisitor
        : InputObjectFieldVisitorBase
    {
        private readonly HashSet<ObjectValueNode> _visited =
            new HashSet<ObjectValueNode>();

        public InputObjectFieldNamesVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitObjectValue(
            InputObjectType type,
            ObjectValueNode objectValue)
        {
            if (_visited.Add(objectValue))
            {
                foreach (ObjectFieldNode fieldValue in objectValue.Fields)
                {
                    if (type.Fields.TryGetField(fieldValue.Name.Value,
                        out InputField inputField))
                    {
                        if (inputField.Type is InputObjectType inputFieldType
                            && fieldValue.Value is ObjectValueNode ov)
                        {
                            VisitObjectValue(inputFieldType, ov);
                        }
                    }
                    else
                    {
                        Errors.Add(new ValidationError(
                            "The specified input object field " +
                            $"`{fieldValue.Name.Value}` does not exist.",
                            fieldValue));
                    }
                }
            }
        }
    }
}
