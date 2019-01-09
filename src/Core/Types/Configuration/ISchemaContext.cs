using System;

namespace HotChocolate.Configuration
{
    internal interface ISchemaContext
    {
        IServiceProvider Services { get; }
        ITypeRegistry Types { get; }
        IDirectiveRegistry Directives { get; }
        IResolverRegistry Resolvers { get; }
    }
}
