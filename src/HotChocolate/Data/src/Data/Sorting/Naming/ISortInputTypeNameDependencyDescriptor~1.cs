using System;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    public interface ISortInputTypeNameDependencyDescriptor<T>
    {
        ISortInputTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType;

        ISortInputTypeDescriptor<T> DependsOn(Type schemaType);
    }
}