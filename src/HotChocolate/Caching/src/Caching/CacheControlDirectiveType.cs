using HotChocolate.Types;

namespace HotChocolate.Caching;

public class CacheControlDirectiveType : DirectiveType<CacheControlDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor<CacheControlDirective> descriptor)
    {
        descriptor
            .Name("cacheControl")
            .Description("TODO")
            .Location(DirectiveLocation.FieldDefinition
                | DirectiveLocation.Object
                | DirectiveLocation.Interface
                | DirectiveLocation.Union);

        descriptor
            .Argument(a => a.MaxAge)
            .Name("maxAge")
            .Description("TODO")
            .Type<IntType>();

        descriptor
            .Argument(a => a.Scope)
            .Name("scope")
            .Description("TODO")
            .Type<CacheControlScopeType>();

        descriptor
            .Argument(a => a.InheritMaxAge)
            .Name("inheritMaxAge")
            .Description("TODO")
            .Type<BooleanType>();
    }
}