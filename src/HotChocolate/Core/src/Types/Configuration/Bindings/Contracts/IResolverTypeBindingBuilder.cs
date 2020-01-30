using System;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Bindings
{
    public interface IResolverTypeBindingBuilder
        : IBindingBuilder
    {
        IResolverTypeBindingBuilder SetType(NameString typeName);
        IResolverTypeBindingBuilder SetType(Type type);
        IResolverTypeBindingBuilder SetResolverType(Type type);
        IResolverTypeBindingBuilder SetFieldBinding(BindingBehavior behavior);
        IResolverTypeBindingBuilder AddField(
            Action<IResolverFieldBindingBuilder> configure);
        IResolverTypeBindingBuilder AddField(
            IResolverFieldBindingBuilder builder);
    }
}
