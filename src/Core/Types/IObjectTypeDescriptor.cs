using System;
using System.Linq.Expressions;

namespace HotChocolate.Types
{
    public interface IObjectTypeDescriptor<T>
        : IObjectTypeDescriptor
    {
        IFieldDescriptor Field<TValue>(Expression<Func<T, TValue>> property);
    }

    public interface IObjectTypeDescriptor
    {
        IObjectTypeDescriptor Name(string name);
        IObjectTypeDescriptor Description(string description);
        IObjectTypeDescriptor Interface<T>()
            where T : InterfaceType;
        IObjectTypeDescriptor IsOfType(IsOfType isOfType);
        IFieldDescriptor Field(string name);
    }
}
