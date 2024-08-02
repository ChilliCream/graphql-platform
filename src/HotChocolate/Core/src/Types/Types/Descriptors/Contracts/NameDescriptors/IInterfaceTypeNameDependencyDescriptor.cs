// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public interface IInterfaceTypeNameDependencyDescriptor
{
    IInterfaceTypeDescriptor DependsOn<TDependency>()
        where TDependency : IType;

    IInterfaceTypeDescriptor DependsOn(Type schemaType);
}
