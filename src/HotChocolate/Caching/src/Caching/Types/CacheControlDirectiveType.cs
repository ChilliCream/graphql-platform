using HotChocolate.Types;
using static HotChocolate.Caching.Properties.CacheControlResources;

namespace HotChocolate.Caching;

public sealed class CacheControlDirectiveType : DirectiveType<CacheControlDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<CacheControlDirective> descriptor)
    {
        descriptor
            .Name(Names.DirectiveName)
            .Description(CacheControlDirectiveType_Description)
            .Location(
                DirectiveLocation.Object |
                DirectiveLocation.FieldDefinition |
                DirectiveLocation.Interface |
                DirectiveLocation.Union);

        descriptor
            .Argument(a => a.MaxAge)
            .Name(Names.MaxAgeArgName)
            .Description(CacheControlDirectiveType_MaxAge)
            .Type<IntType>();

        descriptor
            .Argument(a => a.Scope)
            .Name(Names.ScopeArgName)
            .Description(CacheControlDirectiveType_Scope)
            .Type<CacheControlScopeType>();

        descriptor
            .Argument(a => a.InheritMaxAge)
            .Name(Names.InheritMaxAgeArgName)
            .Description(CacheControlDirectiveType_InheritMaxAge)
            .Type<BooleanType>();
    }

    public static class Names
    {
        public const string DirectiveName = "cacheControl";
        public const string MaxAgeArgName = "maxAge";
        public const string ScopeArgName = "scope";
        public const string InheritMaxAgeArgName = "inheritMaxAge";
    }
}
