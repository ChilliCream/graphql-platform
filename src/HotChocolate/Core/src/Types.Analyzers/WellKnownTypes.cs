namespace HotChocolate.Types.Analyzers;

public static class WellKnownTypes
{
    public const string ModuleAttribute = "HotChocolate.ModuleAttribute";
    public const string SystemObject = "System.Object";
    public const string ObjectType = "HotChocolate.Types.ObjectType";
    public const string InterfaceType = "HotChocolate.Types.InterfaceType";
    public const string UnionType = "HotChocolate.Types.UnionType";
    public const string InputObjectType = "HotChocolate.Types.InputObjectType";
    public const string EnumType = "HotChocolate.Types.EnumType";
    public const string ScalarType = "HotChocolate.Types.ScalarType";
    public const string ObjectTypeExtension = "HotChocolate.Types.ObjectTypeExtension";
    public const string InterfaceTypeExtension = "HotChocolate.Types.InterfaceTypeExtension";
    public const string UnionTypeExtension = "HotChocolate.Types.UnionTypeExtension";
    public const string InputObjectTypeExtension = "HotChocolate.Types.InputObjectTypeExtension";
    public const string EnumTypeExtension = "HotChocolate.Types.EnumTypeExtension";
    public const string DataLoader = "GreenDonut.IDataLoader";

    public static HashSet<string> TypeClass { get; } =
        new()
        {
            ObjectType,
            InterfaceType,
            UnionType,
            InputObjectType,
            EnumType,
            ScalarType,
        };

    public static HashSet<string> TypeExtensionClass { get; } =
        new()
        {
            ObjectTypeExtension,
            InterfaceTypeExtension,
            UnionTypeExtension,
            InputObjectTypeExtension,
            EnumTypeExtension
        };
}
