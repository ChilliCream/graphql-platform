using System;

namespace HotChocolate.Types
{
    internal static class DirectiveHelper
    {
        public static DirectiveLocation InferDirectiveLocation(object source)
        {
            switch (source)
            {
                case ISchema schema:
                    return DirectiveLocation.Schema;
                case ScalarType type:
                    return DirectiveLocation.Scalar;
                case ObjectType type:
                    return DirectiveLocation.Object;
                case InterfaceType type:
                    return DirectiveLocation.Interface;
                case UnionType type:
                    return DirectiveLocation.Union;
                case InputObjectType type:
                    return DirectiveLocation.InputObject;
                case EnumType type:
                    return DirectiveLocation.Enum;
                case IEnumValue value:
                    return DirectiveLocation.EnumValue;
                case IOutputField field:
                    return DirectiveLocation.FieldDefinition;
                case InputField field:
                    return DirectiveLocation.InputFieldDefinition;
                case Argument argument:
                    return DirectiveLocation.ArgumentDefinition;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
