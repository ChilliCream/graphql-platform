#nullable enable

using System.Collections.Generic;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.ApolloFederation.FederationContextData;

namespace HotChocolate.ApolloFederation.Types;

public static class ServerFields
{
    private static readonly object _sdlLock = new();
    private static readonly _Service _service = new();

    internal static ObjectFieldDefinition CreateServiceField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, WellKnownFieldNames.Service);
        descriptor.Type<NonNullType<ObjectType<_Service>>>().Resolve(_service);
        descriptor.Definition.PureResolver = Resolve;

        static _Service Resolve(IPureResolverContext ctx)
            => _service;

        return descriptor.CreateDefinition();
    }

    internal static ObjectFieldDefinition CreateEntitiesField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, WellKnownFieldNames.Entities);
        var maxAllowedRepresentations = context.GetFederationOptions().MaxAllowedRepresentations;

        descriptor
            .Type<NonNullType<ListType<_EntityType>>>()
            .Argument(
                WellKnownArgumentNames.Representations,
                d => d.Type<NonNullType<ListType<NonNullType<_AnyType>>>>())
            .Resolve(c => EntitiesResolver.ResolveAsync(c, maxAllowedRepresentations));

        return descriptor.CreateDefinition();
    }

    internal static string ResolveSdl(IPureResolverContext context)
    {
        bool resolved;

        lock (_sdlLock)
        {
            resolved = context.ContextData.ContainsKey(SdlResolved);
            context.ContextData[SdlResolved] = true;
        }

        if (resolved)
        {
            throw ThrowHelper.Service_SdlResolvedMoreThanOnce(
                context.Selection.SyntaxNode,
                context.Path);
        }

        return context.Parent<_Service>().GetSdl(context.Schema);
    }
}
