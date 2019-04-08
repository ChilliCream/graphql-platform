namespace HotChocolate.Types
{
    public interface IInputObjectTypeNameDependencyDescriptor<T>
    {
        IInputObjectTypeDescriptor<T> DependsOn<TDependency>()
            where TDependency : IType;
    }
}
