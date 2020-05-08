using System;

namespace HotChocolate.Types
{
    public interface IInputUnionTypeNameDependencyDescriptor
    {
        IUnionTypeDescriptor DependsOn<TDependency>()
            where TDependency : IType;

        IUnionTypeDescriptor DependsOn(Type schemaType);
    }
}
