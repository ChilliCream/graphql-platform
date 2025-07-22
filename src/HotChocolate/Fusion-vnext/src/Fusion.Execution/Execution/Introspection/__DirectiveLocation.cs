namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal static class __DirectiveLocation
{
    public static ReadOnlySpan<byte> Query => "QUERY"u8;
    public static ReadOnlySpan<byte> Mutation => "MUTATION"u8;
    public static ReadOnlySpan<byte> Subscription => "SUBSCRIPTION"u8;
    public static ReadOnlySpan<byte> Field => "FIELD"u8;
    public static ReadOnlySpan<byte> FragmentDefinition => "FRAGMENT_DEFINITION"u8;
    public static ReadOnlySpan<byte> FragmentSpread => "FRAGMENT_SPREAD"u8;
    public static ReadOnlySpan<byte> InlineFragment => "INLINE_FRAGMENT"u8;
    public static ReadOnlySpan<byte> VariableDefinition => "VARIABLE_DEFINITION"u8;
    public static ReadOnlySpan<byte> Schema => "SCHEMA"u8;
    public static ReadOnlySpan<byte> Scalar => "SCALAR"u8;
    public static ReadOnlySpan<byte> Object => "OBJECT"u8;
    public static ReadOnlySpan<byte> FieldDefinition => "FIELD_DEFINITION"u8;
    public static ReadOnlySpan<byte> ArgumentDefinition => "ARGUMENT_DEFINITION"u8;
    public static ReadOnlySpan<byte> Interface => "INTERFACE"u8;
    public static ReadOnlySpan<byte> Union => "UNION"u8;
    public static ReadOnlySpan<byte> Enum => "ENUM"u8;
    public static ReadOnlySpan<byte> EnumValue => "ENUM_VALUE"u8;
    public static ReadOnlySpan<byte> InputObject => "INPUT_OBJECT"u8;
    public static ReadOnlySpan<byte> InputFieldDefinition => "INPUT_FIELD_DEFINITION"u8;
}
