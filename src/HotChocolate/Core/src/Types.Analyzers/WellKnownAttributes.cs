namespace HotChocolate.Types.Analyzers;

public static class WellKnownAttributes
{
    public const string ModuleAttribute = "HotChocolate.ModuleAttribute";
    public const string ExtendObjectTypeAttribute = "HotChocolate.Types.ExtendObjectTypeAttribute";
    public const string ObjectTypeAttribute = "HotChocolate.Types.ObjectTypeAttribute";
    public const string InterfaceTypeAttribute = "HotChocolate.Types.InterfaceTypeAttribute";
    public const string UnionTypeAttribute = "HotChocolate.Types.UnionTypeAttribute";
    public const string EnumTypeAttribute = "HotChocolate.Types.EnumTypeAttribute";
    public const string InputObjectTypeAttribute = "HotChocolate.Types.InputObjectTypeAttribute";
    public const string QueryTypeAttribute = "HotChocolate.Types.QueryTypeAttribute";
    public const string MutationTypeAttribute = "HotChocolate.Types.MutationTypeAttribute";
    public const string SubscriptionTypeAttribute = "HotChocolate.Types.SubscriptionTypeAttribute";
    public const string DataLoaderAttribute = "GreenDonut.DataLoaderAttribute";
    public const string DataLoaderModuleAttribute = "GreenDonut.DataLoaderModuleAttribute";
    public const string DataLoaderDefaultsAttribute = "GreenDonut.DataLoaderDefaultsAttribute";
    public const string DataLoaderStateAttribute = "GreenDonut.DataLoaderStateAttribute";
    public const string QueryAttribute = "HotChocolate.QueryAttribute";
    public const string MutationAttribute = "HotChocolate.MutationAttribute";
    public const string SubscriptionAttribute = "HotChocolate.SubscriptionAttribute";
    public const string NodeResolverAttribute = "HotChocolate.Types.Relay.NodeResolverAttribute";
    public const string ParentAttribute = "HotChocolate.ParentAttribute";
    public const string EventMessageAttribute = "HotChocolate.EventMessageAttribute";
    public const string ServiceAttribute = "HotChocolate.ServiceAttribute";
    public const string ArgumentAttribute = "HotChocolate.ArgumentAttribute";
    public const string BindMemberAttribute = "HotChocolate.Types.BindMemberAttribute";
    public const string BindFieldAttribute = "HotChocolate.Types.BindFieldAttribute";

    public static HashSet<string> BindAttributes { get; } =
    [
        BindMemberAttribute,
        BindFieldAttribute,
    ];

    public static HashSet<string> TypeAttributes { get; } =
    [
        ExtendObjectTypeAttribute,
        ObjectTypeAttribute,
        InterfaceTypeAttribute,
        UnionTypeAttribute,
        EnumTypeAttribute,
        InputObjectTypeAttribute,
        QueryTypeAttribute,
        MutationTypeAttribute,
        SubscriptionTypeAttribute,
    ];
}
