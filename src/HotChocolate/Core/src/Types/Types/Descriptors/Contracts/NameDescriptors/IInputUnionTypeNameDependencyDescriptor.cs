using System;

namespace HotChocolate.Types
{
    public interface IInputUnionTypeNameDependencyDescriptor
    {
        IInputUnionTypeDescriptor DependsOn<TDependency>()
            where TDependency : IType;

        IInputUnionTypeDescriptor DependsOn(Type schemaType);
    }
}
