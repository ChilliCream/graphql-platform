namespace HotChocolate.Types
{
    public interface IInputObjectTypeNameDependencyDescriptor
    {
        IInputObjectTypeDescriptor DependsOn<TDependency>()
            where TDependency : IType;
    }
}
