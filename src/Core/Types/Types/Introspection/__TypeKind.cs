namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __TypeKind
        : EnumType<TypeKind>
    {
        protected override void Configure(IEnumTypeDescriptor<TypeKind> descriptor)
        {
            descriptor.Name("__TypeKind");

            descriptor.Description(
                "An enum describing what kind of type a given `__Type` is.");

            descriptor.Item(TypeKind.Scalar)
                .Description("Indicates this type is a scalar.");

            descriptor.Item(TypeKind.Object)
                .Description("Indicates this type is an object. " +
                    "`fields` and `interfaces` are valid fields.");

            descriptor.Item(TypeKind.Interface)
                .Description("Indicates this type is an interface. " +
                    "`fields` and `possibleTypes` are valid fields.");

            descriptor.Item(TypeKind.Union)
                .Description("Indicates this type is a union. " +
                    "`possibleTypes` is a valid field.");

            descriptor.Item(TypeKind.Enum)
                .Description("Indicates this type is an enum. " +
                    "`enumValues` is a valid field.");

            descriptor.Item(TypeKind.InputObject)
                .Name("INPUT_OBJECT")
                .Description("Indicates this type is an input object. " +
                    "`inputFields` is a valid field.");

            descriptor.Item(TypeKind.List)
                .Description("Indicates this type is a list. " +
                    "`ofType` is a valid field.");

            descriptor.Item(TypeKind.NonNull)
                .Name("NON_NULL")
                .Description("Indicates this type is a non-null. " +
                    "`ofType` is a valid field.");
        }
    }
}
