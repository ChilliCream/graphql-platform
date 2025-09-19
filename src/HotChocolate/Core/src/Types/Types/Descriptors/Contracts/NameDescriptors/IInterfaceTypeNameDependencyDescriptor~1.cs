using HotChocolate.Types.Descriptors;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public interface IInterfaceTypeNameDependencyDescriptor<T>
{
    IInterfaceTypeDescriptor<T> DependsOn<TDependency>()
        where TDependency : IType;

    IInterfaceTypeDescriptor<T> DependsOn(Type schemaType);

    IInterfaceTypeDescriptor<T> DependsOn(TypeReference typeReference);
}
