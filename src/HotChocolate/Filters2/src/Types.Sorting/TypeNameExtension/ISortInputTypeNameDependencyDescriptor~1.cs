using System;

namespace HotChocolate.Types.Sorting
{
    public interface ISortInputTypeNameDependencyDescriptor<T>
    {
        ISortInputTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType;

        ISortInputTypeDescriptor<T> DependsOn(Type schemaType);
    }
}
