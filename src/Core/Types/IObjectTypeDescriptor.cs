using System;
using System.Linq.Expressions;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public interface IObjectTypeDescriptor
    {
        IObjectTypeDescriptor Name(string name);
        IObjectTypeDescriptor Description(string description);
        IObjectTypeDescriptor Interface<T>()
            where T : InterfaceType;
        IObjectTypeDescriptor IsOfType(IsOfType isOfType);
        IFieldDescriptor Field(string name);
    }

    public interface IObjectTypeDescriptor<T>
        : IObjectTypeDescriptor
    {
        IObjectTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior);
        IFieldDescriptor Field<TValue>(Expression<Func<T, TValue>> property);
    }
}
