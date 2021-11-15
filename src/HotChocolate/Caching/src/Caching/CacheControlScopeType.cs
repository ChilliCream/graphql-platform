using HotChocolate.Types;

namespace HotChocolate.Caching;

public class CacheControlScopeType : EnumType<CacheControlScope>
{
    protected override void Configure(IEnumTypeDescriptor<CacheControlScope> descriptor)
    {
        descriptor
            .Name("CacheControlScope")
            .Description("TODO");
    }
}