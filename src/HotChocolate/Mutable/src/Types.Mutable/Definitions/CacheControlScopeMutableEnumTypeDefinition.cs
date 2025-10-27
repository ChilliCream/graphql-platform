namespace HotChocolate.Types.Mutable.Definitions;

public sealed class CacheControlScopeMutableEnumTypeDefinition : MutableEnumTypeDefinition
{
    public CacheControlScopeMutableEnumTypeDefinition()
        : base(WellKnownTypeNames.CacheControlScope)
    {
        Values.Add(new MutableEnumValue("PRIVATE"));
        Values.Add(new MutableEnumValue("PUBLIC"));
    }

    public static CacheControlScopeMutableEnumTypeDefinition Create()
    {
        return new CacheControlScopeMutableEnumTypeDefinition();
    }
}
