using HotChocolate.Types.Descriptors;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public interface IObjectTypeNameDependencyDescriptor
{
    IObjectTypeDescriptor DependsOn<TDependency>()
        where TDependency : IType;

    IObjectTypeDescriptor DependsOn(Type schemaType);

    IObjectTypeDescriptor DependsOn(TypeReference typeReference);
}
