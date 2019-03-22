using System;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Bindings
{
    public interface IComplextTypeBindingBuilder
       : IBindingBuilder
    {
        IComplextTypeBindingBuilder SetName(NameString typeName);
        IComplextTypeBindingBuilder SetType(Type type);
        IResolverTypeBindingBuilder SetFieldBinding(BindingBehavior behavior);
        IComplextTypeBindingBuilder AddField(
            Action<IComplextTypeFieldBindingBuilder> configure);
        IComplextTypeBindingBuilder AddField(
            IComplextTypeFieldBindingBuilder builder);
    }
}
