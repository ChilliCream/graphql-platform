#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __TypeKind : EnumType<TypeKind>
    {
        protected override void Configure(IEnumTypeDescriptor<TypeKind> descriptor)
        {
            descriptor
                .Name(Names.__TypeKind)
                .Description(TypeResources.TypeKind_Description)
                // Introspection types must always be bound explicitly so that we
                // do not get any interference with conventions.
                .BindValues(BindingBehavior.Explicit);

            descriptor
                .Value(TypeKind.Scalar)
                .Name(Names.Scalar)
                .Description(TypeResources.TypeKind_Scalar);

            descriptor
                .Value(TypeKind.Object)
                .Name(Names.Object)
                .Description(TypeResources.TypeKind_Object);

            descriptor
                .Value(TypeKind.Interface)
                .Name(Names.Interface)
                .Description(TypeResources.TypeKind_Interface);

            descriptor
                .Value(TypeKind.Union)
                .Name(Names.Union)
                .Description(TypeResources.TypeKind_Union);

            descriptor
                .Value(TypeKind.Enum)
                .Name(Names.Enum)
                .Description(TypeResources.TypeKind_Enum);

            descriptor
                .Value(TypeKind.InputObject)
                .Name(Names.InputObject)
                .Description(TypeResources.TypeKind_InputObject);

            descriptor
                .Value(TypeKind.List)
                .Name(Names.List)
                .Description(TypeResources.TypeKind_List);

            descriptor
                .Value(TypeKind.NonNull)
                .Name(Names.NonNull)
                .Description(TypeResources.TypeKind_NonNull);
        }

        public static class Names
        {
            public const string __TypeKind = "__TypeKind";
            public const string Scalar = "SCALAR";
            public const string Object = "OBJECT";
            public const string Interface = "INTERFACE";
            public const string Union = "UNION";
            public const string Enum = "ENUM";
            public const string InputObject = "INPUT_OBJECT";
            public const string List = "LIST";
            public const string NonNull = "NON_NULL";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
