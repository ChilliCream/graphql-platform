using System;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public interface IFilterInputTypeNameDependencyDescriptor<T>
    {
        IFilterInputTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType;

        IFilterInputTypeDescriptor<T> DependsOn(Type schemaType);
    }
}
