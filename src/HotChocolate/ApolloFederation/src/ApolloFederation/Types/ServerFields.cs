using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.Features;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.ApolloFederation.FederationContextData;

namespace HotChocolate.ApolloFederation.Types;

public static class ServerFields
{
#if NET9_0_OR_GREATER
    private static readonly Lock s_sdlLock = new();
#else
    private static readonly object s_sdlLock = new();
#endif
    private static readonly _Service s_service = new();

    internal static ObjectFieldConfiguration CreateServiceField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, WellKnownFieldNames.Service);
        descriptor.Type<NonNullType<ObjectType<_Service>>>().Resolve(s_service);
        descriptor.Configuration.PureResolver = Resolve;

        static _Service Resolve(IResolverContext _)
            => s_service;

        return descriptor.CreateConfiguration();
    }

    internal static ObjectFieldConfiguration CreateEntitiesField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, WellKnownFieldNames.Entities);
        var options = context.Features.GetRequired<ApolloFederationFeature>().Options;
        var maxAllowedRepresentations = options.MaxAllowedRepresentations;

        descriptor
            .Type<NonNullType<ListType<_EntityType>>>()
            .Argument(
                WellKnownArgumentNames.Representations,
                d => d.Type<NonNullType<ListType<NonNullType<_AnyType>>>>())
            .Resolve(c => EntitiesResolver.ResolveAsync(c, maxAllowedRepresentations));

        return descriptor.CreateConfiguration();
    }

    internal static string ResolveSdl(IResolverContext context)
    {
        bool resolved;

        lock (s_sdlLock)
        {
            resolved = context.ContextData.ContainsKey(SdlResolved);
            context.ContextData[SdlResolved] = true;
        }

        if (resolved)
        {
            throw ThrowHelper.Service_SdlResolvedMoreThanOnce();
        }

        return context.Parent<_Service>().GetSdl(context.Schema);
    }
}
