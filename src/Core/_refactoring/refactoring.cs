using System;
using System.Collections.Generic;
using System.Reflection;
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
        void RegisterType(INamedType namedType, TypeBinding typeBinding = null);
        void RegisterType(Type nativeType);

        T GetType<T>(string typeName) where T : IType;
        T GetType<T>(Type nativeType) where T : IType;

        bool TryGetTypeBinding(string typeName, out TypeBinding typeBinding);
        bool TryGetTypeBinding(INamedType namedType, out TypeBinding typeBinding);
    }

    public interface IResolverRegistry
    {
        void RegisterResolver(ResolverBinding resolverBinding);
        FieldResolverDelegate GetResolver(string typeName, string fieldName);
    }

    public class TypeBinding
    {
        public string TypeName { get; }

        public Type NativeType { get; }

        public IReadOnlyDictionary<string, TypeMemberBinding> Members { get; }
    }

    public class TypeMemberBinding
    {
        public MemberInfo Member { get; }
    }
}
