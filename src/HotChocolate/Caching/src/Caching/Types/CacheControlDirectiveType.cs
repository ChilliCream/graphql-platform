using HotChocolate.Types;
using static HotChocolate.Caching.Properties.CacheControlResources;

namespace HotChocolate.Caching;

public class CacheControlDirectiveType : DirectiveType<CacheControlDirective>
{
    public const string DirectiveName = "cacheControl";

    protected override void Configure(IDirectiveTypeDescriptor<CacheControlDirective> descriptor)
    {
        descriptor
            .Name(DirectiveName)
            .Description(CacheControlDirectiveType_Description)
            .Location(DirectiveLocation.Object
                | DirectiveLocation.FieldDefinition
                | DirectiveLocation.Interface
                | DirectiveLocation.Union);

        descriptor
            .Argument(a => a.MaxAge)
            .Name("maxAge")
            .Description(CacheControlDirectiveType_MaxAge)
            .Type<IntType>();

        descriptor
            .Argument(a => a.Scope)
            .Name("scope")
            .Description(CacheControlDirectiveType_Scope)
            .Type<CacheControlScopeType>();

        descriptor
            .Argument(a => a.InheritMaxAge)
            .Name("inheritMaxAge")
            .Description(CacheControlDirectiveType_InheritMaxAge)
            .Type<BooleanType>();
    }
}
