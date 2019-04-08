namespace HotChocolate.Types
{
    public interface IUnionTypeNameDependencyDescriptor
    {
        IUnionTypeDescriptor DependsOn<TDependency>()
            where TDependency : IType;
    }
}
