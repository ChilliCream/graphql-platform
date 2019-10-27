using System;

namespace HotChocolate.Types.Filters
{
    public interface IFilterInputTypeNameDependencyDescriptor<T>
    {
        IFilterInputTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType;

        IFilterInputTypeDescriptor<T> DependsOn(Type schemaType);
    }
}
