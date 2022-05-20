using HotChocolate.Types;

namespace HotChocolate.Caching;

public class CacheControlDirectiveType : DirectiveType<CacheControlDirective>
{
    public const string DirectiveName = "cacheControl";

    protected override void Configure(IDirectiveTypeDescriptor<CacheControlDirective> descriptor)
    {
        descriptor
            .Name(DirectiveName)
            .Description("TODO")
            .Location(DirectiveLocation.Object
                | DirectiveLocation.FieldDefinition
                | DirectiveLocation.Interface
                | DirectiveLocation.Union
                );

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
