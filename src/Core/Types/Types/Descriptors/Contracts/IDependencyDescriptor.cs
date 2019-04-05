namespace HotChocolate.Types
{
    public interface IDependencyDescriptor
    {
        IDependencyDescriptor DependsOn<T>()
            where T : ITypeSystem;

        IDependencyDescriptor DependsOn<T>(bool mustBeNamed)
            where T : ITypeSystem;

        IDependencyDescriptor DependsOn(NameString typeName);

        IDependencyDescriptor DependsOn(
            NameString typeName,
            bool mustBeNamed);
    }
}
