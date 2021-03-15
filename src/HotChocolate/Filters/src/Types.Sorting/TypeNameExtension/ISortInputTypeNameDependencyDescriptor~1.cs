using System;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public interface ISortInputTypeNameDependencyDescriptor<T>
    {
        ISortInputTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType;

        ISortInputTypeDescriptor<T> DependsOn(Type schemaType);
    }
}
