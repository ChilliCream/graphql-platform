using System;

namespace HotChocolate.Types
{
    public interface IInputObjectTypeNameDependencyDescriptor<T>
    {
        IInputObjectTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType;

        IInputObjectTypeDescriptor<T> DependsOn(Type schemaType);
    }
}
