using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Fusion;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class AbstractTypeMockingTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, TypeSystemConfiguration configuration)
    {
        if (configuration is InterfaceTypeConfiguration interfaceTypeConfiguration)
        {
            interfaceTypeConfiguration.ResolveAbstractType = ResolveAbstractType;
        }
        else if (configuration is UnionTypeConfiguration unionTypeConfiguration)
        {
            unionTypeConfiguration.ResolveAbstractType = ResolveAbstractType;
        }
    }

    private ObjectType ResolveAbstractType(IResolverContext context, object resolverResult)
    {
        if (resolverResult is ObjectTypeInst inst)
        {
            return inst.Type;
        }

        throw new InvalidOperationException();
    }
}
