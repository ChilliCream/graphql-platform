namespace HotChocolate.Types
{
    public interface IEnumTypeNameDependencyDescriptor
    {
        IEnumTypeDescriptor DependsOn<TDependency>()
            where TDependency : IType;
    }
}
