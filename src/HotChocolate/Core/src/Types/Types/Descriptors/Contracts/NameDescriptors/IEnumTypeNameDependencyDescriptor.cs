// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public interface IEnumTypeNameDependencyDescriptor
{
    IEnumTypeDescriptor DependsOn<TDependency>()
        where TDependency : IType;

    IEnumTypeDescriptor DependsOn(Type schemaType);
}
