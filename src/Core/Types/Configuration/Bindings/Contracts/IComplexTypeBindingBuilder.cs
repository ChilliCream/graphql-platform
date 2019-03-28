using System;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Bindings
{
    public interface IComplexTypeBindingBuilder
       : IBindingBuilder
    {
        IComplexTypeBindingBuilder SetName(NameString typeName);
        IComplexTypeBindingBuilder SetType(Type type);
        IComplexTypeBindingBuilder SetFieldBinding(BindingBehavior behavior);
        IComplexTypeBindingBuilder AddField(
            Action<IComplexTypeFieldBindingBuilder> configure);
        IComplexTypeBindingBuilder AddField(
            IComplexTypeFieldBindingBuilder builder);
    }
}
