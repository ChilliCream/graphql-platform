using HotChocolate.Types;
using static HotChocolate.Caching.Properties.CacheControlResources;

namespace HotChocolate.Caching;

public sealed class CacheControlScopeType
    : EnumType<CacheControlScope>
{
    protected override void Configure(
        IEnumTypeDescriptor<CacheControlScope> descriptor)
    {
        descriptor
            .Name("CacheControlScope")
            .Description(CacheControlScopeType_Description);

        descriptor
            .Value(CacheControlScope.Public)
            .Description(CacheControlScopeType_Public);

        descriptor
            .Value(CacheControlScope.Private)
            .Description(CacheControlScopeType_Private);
    }
}
