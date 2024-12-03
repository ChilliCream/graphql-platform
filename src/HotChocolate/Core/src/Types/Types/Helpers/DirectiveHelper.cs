namespace HotChocolate.Types.Helpers;

internal static class DirectiveHelper
{
    public static DirectiveLocation InferDirectiveLocation(object source)
    {
        return source switch
        {
            ISchema => DirectiveLocation.Schema,
            ScalarType => DirectiveLocation.Scalar,
            ObjectType => DirectiveLocation.Object,
            InterfaceType => DirectiveLocation.Interface,
            UnionType => DirectiveLocation.Union,
            InputObjectType => DirectiveLocation.InputObject,
            EnumType => DirectiveLocation.Enum,
            IEnumValue => DirectiveLocation.EnumValue,
            IOutputField => DirectiveLocation.FieldDefinition,
            InputField => DirectiveLocation.InputFieldDefinition,
            Argument => DirectiveLocation.ArgumentDefinition,
            _ => throw new NotSupportedException(),
        };
    }
}
