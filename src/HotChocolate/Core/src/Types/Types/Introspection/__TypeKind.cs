using HotChocolate.Properties;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __TypeKind
        : EnumType<TypeKind>
    {
        protected override void Configure(IEnumTypeDescriptor<TypeKind> descriptor)
        {
            descriptor.Name("__TypeKind");

            descriptor.Description(TypeResources.TypeKind_Description);

            descriptor.Item(TypeKind.Scalar)
                .Description(TypeResources.TypeKind_Scalar);

            descriptor.Item(TypeKind.Object)
                .Description(TypeResources.TypeKind_Object);

            descriptor.Item(TypeKind.Interface)
                .Description(TypeResources.TypeKind_Interface);

            descriptor.Item(TypeKind.Union)
                .Description(TypeResources.TypeKind_Union);

            descriptor.Item(TypeKind.Enum)
                .Description(TypeResources.TypeKind_Enum);

            descriptor.Item(TypeKind.InputObject)
                .Name("INPUT_OBJECT")
                .Description(TypeResources.TypeKind_InputObject);

            descriptor.Item(TypeKind.List)
                .Description(TypeResources.TypeKind_List);

            descriptor.Item(TypeKind.NonNull)
                .Name("NON_NULL")
                .Description(TypeResources.TypeKind_NonNull);
        }
    }
}
