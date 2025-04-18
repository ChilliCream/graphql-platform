using HotChocolate.Types.Descriptors;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public interface IObjectTypeNameDependencyDescriptor<T>
{
    IObjectTypeDescriptor<T> DependsOn<TDependency>()
        where TDependency : IType;

    IObjectTypeDescriptor<T> DependsOn(Type schemaType);

    IObjectTypeDescriptor<T> DependsOn(TypeReference typeReference);
}
