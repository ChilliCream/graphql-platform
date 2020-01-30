using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration.Bindings
{
    public interface IResolverFieldBindingBuilder
        : IBindingBuilder
    {
        IResolverFieldBindingBuilder SetField(NameString fieldName);
        IResolverFieldBindingBuilder SetField(MemberInfo member);
        IResolverFieldBindingBuilder SetResolver(MemberInfo member);
        IResolverFieldBindingBuilder SetResolver(FieldResolverDelegate resolver);
    }
}
