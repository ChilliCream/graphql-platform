// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public interface IUnionTypeNameDependencyDescriptor
{
    IUnionTypeDescriptor DependsOn<TDependency>()
        where TDependency : IType;

    IUnionTypeDescriptor DependsOn(Type schemaType);
}
