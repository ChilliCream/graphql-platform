using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class CacheControlScopeMutableEnumTypeDefinition : MutableEnumTypeDefinition
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
