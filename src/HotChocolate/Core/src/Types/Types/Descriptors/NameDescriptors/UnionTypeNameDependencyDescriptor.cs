using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

internal class UnionTypeNameDependencyDescriptor
    : IUnionTypeNameDependencyDescriptor
{
    private readonly IUnionTypeDescriptor _descriptor;
    private readonly Func<INamedType, string> _createName;

    public UnionTypeNameDependencyDescriptor(
        IUnionTypeDescriptor descriptor,
        Func<INamedType, string> createName)
    {
        _descriptor = descriptor
            ?? throw new ArgumentNullException(nameof(descriptor));
        _createName = createName
            ?? throw new ArgumentNullException(nameof(createName));
    }

    public IUnionTypeDescriptor DependsOn<TDependency>()
        where TDependency : IType
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeof(TDependency));
        return _descriptor;
    }

    public IUnionTypeDescriptor DependsOn(Type schemaType)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, schemaType);
        return _descriptor;
    }
}
