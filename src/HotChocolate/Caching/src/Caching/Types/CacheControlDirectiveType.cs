using HotChocolate.Types;
using static HotChocolate.Caching.Properties.CacheControlResources;

namespace HotChocolate.Caching;

public sealed class CacheControlDirectiveType
    : DirectiveType<CacheControlDirective>
{
    public const string DirectiveName = "cacheControl";
    public const string MaxAgeArgName = "maxAge";
    public const string ScopeArgName = "scope";
    public const string InheritMaxAgeArgName = "inheritMaxAge";

    protected override void Configure(
        IDirectiveTypeDescriptor<CacheControlDirective> descriptor)
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
            .Name(MaxAgeArgName)
            .Description(CacheControlDirectiveType_MaxAge)
            .Type<IntType>();

        descriptor
            .Argument(a => a.Scope)
            .Name(ScopeArgName)
            .Description(CacheControlDirectiveType_Scope)
            .Type<CacheControlScopeType>();

        descriptor
            .Argument(a => a.InheritMaxAge)
            .Name(InheritMaxAgeArgName)
            .Description(CacheControlDirectiveType_InheritMaxAge)
            .Type<BooleanType>();
    }
}
