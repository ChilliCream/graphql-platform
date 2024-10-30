namespace HotChocolate.Types;

public interface ICompletedDependencyDescriptor
{
    ICompletedDependencyDescriptor DependsOn<T>()
        where T : ITypeSystemMember;

    ICompletedDependencyDescriptor DependsOn<T>(bool mustBeCompleted)
        where T : ITypeSystemMember;

    ICompletedDependencyDescriptor DependsOn(Type schemaType);

    ICompletedDependencyDescriptor DependsOn(
        Type schemaType, bool mustBeCompleted);

    ICompletedDependencyDescriptor DependsOn(string typeName);

    ICompletedDependencyDescriptor DependsOn(string typeName, bool mustBeCompleted);
}
