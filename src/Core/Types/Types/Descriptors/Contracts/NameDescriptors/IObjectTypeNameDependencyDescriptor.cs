namespace HotChocolate.Types
{
    public interface IObjectTypeNameDependencyDescriptor
    {
        IObjectTypeDescriptor DependsOn<TDependency>()
            where TDependency : IType;
    }
}
