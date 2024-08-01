// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public interface IEnumTypeNameDependencyDescriptor<T>
{
    IEnumTypeDescriptor<T> DependsOn<TDependency>()
        where TDependency : IType;

    IEnumTypeDescriptor<T> DependsOn(Type schemaType);
}
