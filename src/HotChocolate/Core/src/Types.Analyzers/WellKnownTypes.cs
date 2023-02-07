namespace HotChocolate.Types.Analyzers;

public static class WellKnownTypes
{
    public const string ModuleAttribute = "HotChocolate.ModuleAttribute";
    public const string DataLoaderDefaultsAttribute = "HotChocolate.DataLoaderDefaultsAttribute";
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
    public const string CancellationToken = "System.Threading.CancellationToken";
    public const string GlobalCancellationToken = "global::System.Threading.CancellationToken";
    public const string ReadOnlyList = "System.Collections.Generic.IReadOnlyList";
    public const string ReadOnlyDictionary = "System.Collections.Generic.IReadOnlyDictionary";
    public const string Lookup = "System.Linq.ILookup";
    public const string Task = "System.Threading.Tasks.Task";

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
