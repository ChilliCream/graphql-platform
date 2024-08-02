// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public interface IInputObjectTypeNameDependencyDescriptor
{
    IInputObjectTypeDescriptor DependsOn<TDependency>()
        where TDependency : IType;

    IInputObjectTypeDescriptor DependsOn(Type schemaType);
}
