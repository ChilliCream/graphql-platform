using System;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration.Bindings
{
    public interface IResolverBindingBuilder
        : IBindingBuilder
    {
        IResolverBindingBuilder SetResolver(FieldResolverDelegate resolver);
        IResolverBindingBuilder SetType(NameString typeName);
        IResolverBindingBuilder SetType(Type type);
        IResolverBindingBuilder SetField(NameString fieldName);
        IResolverBindingBuilder SetField(MemberInfo member);
    }
}
