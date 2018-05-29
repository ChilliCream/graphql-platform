using System;
using System.Linq.Expressions;

namespace HotChocolate.Types
{
    public interface IInputObjectTypeDescriptor
    {
        IInputObjectTypeDescriptor Name(string name);
        IInputObjectTypeDescriptor Description(string name);
    }

    public interface IInputObjectTypeDescriptor<T>
        : IInputObjectTypeDescriptor
    {
        IInputFieldDescriptor Field<TValue>(Expression<Func<T, TValue>> property);
    }
}
