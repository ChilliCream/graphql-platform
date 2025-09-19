using HotChocolate.Types.Descriptors;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public interface IInputObjectTypeNameDependencyDescriptor<T>
{
    IInputObjectTypeDescriptor<T> DependsOn<TDependency>()
        where TDependency : IType;

    IInputObjectTypeDescriptor<T> DependsOn(Type schemaType);

    IInputObjectTypeDescriptor<T> DependsOn(TypeReference typeReference);
}
