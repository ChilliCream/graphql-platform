using System;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public interface IFilterInputTypeNameDependencyDescriptor<T>
    {
        IFilterInputTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType;

        IFilterInputTypeDescriptor<T> DependsOn(Type schemaType);
    }
}