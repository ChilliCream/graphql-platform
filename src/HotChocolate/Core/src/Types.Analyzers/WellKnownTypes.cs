namespace HotChocolate.Types.Analyzers;

public static class WellKnownTypes
{
    public const string ModuleName = "HotChocolate.ModuleNameAttribute";
    public const string SystemObject = "System.Object";
    public const string ObjectType = "HotChocolate.Types.ObjectType";
    public const string InterfaceType = "HotChocolate.Types.InterfaceType";
    public const string UnionType = "HotChocolate.Types.UnionType";
    public const string InputObjectType = "HotChocolate.Types.InputObjectType";
    public const string EnumType = "HotChocolate.Types.EnumType";
    public const string ScalarType = "HotChocolate.Types.ScalarType";

    public static HashSet<string> BaseTypes { get; } =
        new HashSet<string>
        {
            ObjectType,
            InterfaceType,
            UnionType,
            InputObjectType,
            EnumType,
            ScalarType,
        };
}
