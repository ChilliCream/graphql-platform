using System;
using System.Linq.Expressions;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public interface IInputObjectTypeDescriptor
        : IFluent
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
