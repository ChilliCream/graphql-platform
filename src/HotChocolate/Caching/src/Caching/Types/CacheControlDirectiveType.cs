using HotChocolate.Types;
using static HotChocolate.Caching.Properties.CacheControlResources;

namespace HotChocolate.Caching;

/// <summary>
/// The `@cacheControl` directive may be provided for individual fields or
/// entire object, interface or union types to provide caching hints to
/// the executor.
/// </summary>
public sealed class CacheControlDirectiveType : DirectiveType<CacheControlDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor<CacheControlDirective> descriptor)
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
            .Argument(a => a.SharedMaxAge)
            .Name(Names.SharedMaxAgeArgName)
            .Description(CacheControlDirectiveType_SharedMaxAge)
            .Type<IntType>();

        descriptor
            .Argument(a => a.InheritMaxAge)
            .Name(Names.InheritMaxAgeArgName)
            .Description(CacheControlDirectiveType_InheritMaxAge)
            .Type<BooleanType>();

        descriptor
            .Argument(a => a.Scope)
            .Name(Names.ScopeArgName)
            .Description(CacheControlDirectiveType_Scope)
            .Type<CacheControlScopeType>();

        descriptor
            .Argument(a => a.Vary)
            .Name(Names.VaryArgName)
            .Description(CacheControlDirectiveType_Vary)
            .Type<ListType<StringType>>();
    }

    public static class Names
    {
        public const string DirectiveName = "cacheControl";
        public const string MaxAgeArgName = "maxAge";
        public const string SharedMaxAgeArgName = "sharedMaxAge";
        public const string InheritMaxAgeArgName = "inheritMaxAge";
        public const string ScopeArgName = "scope";
        public const string VaryArgName = "vary";
    }
}
