using System;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    public interface ISchemaContextR
    {
        ITypeRegistry Types { get; }
        IResolverRegistry Resolvers { get; }
    }

    public interface ITypeRegistry
    {
        void RegisterType(INamedType namedType, EntityTypeBinding entityTypeBinding = null);
        void RegisterType(Type nativeType);
        T GetType<T>(string typeName) where T : IType;
        T GetType<T>(Type nativeType) where T : IType;
    }

    public interface IResolverRegistry
    {
        void RegisterResolver(ResolverBinding resolverBinding);
        FieldResolverDelegate GetResolver(string typeName, string fieldName);
    }

    public abstract class EntityTypeBinding
    {

    }
}
