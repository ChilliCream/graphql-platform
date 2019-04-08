namespace HotChocolate.Types
{
    public interface IInterfaceTypeNameDependencyDescriptor<T>
    {
        IInterfaceTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType;
    }
}
