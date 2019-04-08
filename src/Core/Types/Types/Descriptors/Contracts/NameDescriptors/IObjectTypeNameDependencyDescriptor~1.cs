namespace HotChocolate.Types
{
    public interface IObjectTypeNameDependencyDescriptor<T>
    {
        IObjectTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType;
    }
}
