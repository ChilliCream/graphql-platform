using HotChocolate.Types;

namespace HotChocolate.Caching;

public class CacheControlScopeType : EnumType<CacheControlScope>
{
    protected override void Configure(IEnumTypeDescriptor<CacheControlScope> descriptor)
    {
        descriptor
            .Name("CacheControlScope")
            .Description("TODO");

        descriptor
            .Value(CacheControlScope.Public)
            .Description("TODO");

        descriptor
            .Value(CacheControlScope.Private)
            .Description("TODO");
    }
}