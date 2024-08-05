using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors;

internal class CompletedDependencyDescriptor
    : DependencyDescriptorBase
    , ICompletedDependencyDescriptor
{
    public CompletedDependencyDescriptor(
        ITypeInspector typeInspector,
        CompleteConfiguration configuration)
        : base(typeInspector, configuration)
    {
    }

    protected override TypeDependencyFulfilled DependencyFulfilled =>
        TypeDependencyFulfilled.Completed;

    public ICompletedDependencyDescriptor DependsOn<TType>()
        where TType : ITypeSystemMember =>
        DependsOn<TType>(false);

    public new ICompletedDependencyDescriptor DependsOn<TType>(
        bool mustBeCompleted)
        where TType : ITypeSystemMember
    {
        base.DependsOn<TType>(mustBeCompleted);
        return this;
    }

    public ICompletedDependencyDescriptor DependsOn(Type schemaType) =>
        DependsOn(schemaType, false);

    public new ICompletedDependencyDescriptor DependsOn(
        Type schemaType, bool mustBeCompleted)
    {
        base.DependsOn(schemaType, mustBeCompleted);
        return this;
    }

    public ICompletedDependencyDescriptor DependsOn(
        string typeName)
        => DependsOn(typeName, false);

    public new ICompletedDependencyDescriptor DependsOn(
        string typeName,
        bool mustBeCompleted)
    {
        base.DependsOn(typeName, mustBeCompleted);
        return this;
    }
}
