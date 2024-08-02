namespace HotChocolate.Types;

public interface INamedDependencyDescriptor
{
    INamedDependencyDescriptor DependsOn<T>()
        where T : ITypeSystemMember;

    INamedDependencyDescriptor DependsOn<T>(bool mustBeNamed)
        where T : ITypeSystemMember;

    INamedDependencyDescriptor DependsOn(Type schemaType);

    INamedDependencyDescriptor DependsOn(Type schemaType, bool mustBeNamed);

    INamedDependencyDescriptor DependsOn(string typeName);

    INamedDependencyDescriptor DependsOn(string typeName, bool mustBeNamed);
}
