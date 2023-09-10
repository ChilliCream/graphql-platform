#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __TypeKind : EnumType
{
    protected override EnumTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        => new(Names.__TypeKind, TypeKind_Description, typeof(TypeKind))
        {
            Values =
            {
                new(Names.Scalar, TypeKind_Scalar, TypeKind.Scalar),
                new(Names.Object, TypeKind_Object, TypeKind.Object),
                new(Names.Interface, TypeKind_Interface, TypeKind.Interface),
                new(Names.Union, TypeKind_Union, TypeKind.Union),
                new(Names.Enum, TypeKind_Enum, TypeKind.Enum),
                new(Names.InputObject, TypeKind_InputObject, TypeKind.InputObject),
                new(Names.List, TypeKind_List, TypeKind.List),
                new(Names.NonNull, TypeKind_NonNull, TypeKind.NonNull),
            }
        };

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
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
#pragma warning restore IDE1006 // Naming Styles
