namespace HotChocolate.Types.Analyzers;

public static class WellKnownAttributes
{
    public const string ExtendObjectTypeAttribute = "HotChocolate.Types.ExtendObjectTypeAttribute";
    public const string ObjectTypeAttribute = "HotChocolate.Types.ObjectTypeAttribute";
    public const string InterfaceTypeAttribute = "HotChocolate.Types.InterfaceTypeAttribute";
    public const string UnionTypeAttribute = "HotChocolate.Types.UnionTypeAttribute";
    public const string EnumTypeAttribute = "HotChocolate.Types.EnumTypeAttribute";
    public const string InputObjectTypeAttribute = "HotChocolate.Types.InputObjectTypeAttribute";

    public static HashSet<string> TypeAttributes { get; } =
        new()
        {
            ExtendObjectTypeAttribute,
            ObjectTypeAttribute,
            InterfaceTypeAttribute,
            UnionTypeAttribute,
            EnumTypeAttribute,
            InputObjectTypeAttribute
        };
}
