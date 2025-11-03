using HotChocolate.Types.Helpers;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors;

internal sealed class UnionTypeNameDependencyDescriptor
    : IUnionTypeNameDependencyDescriptor
{
    private readonly IUnionTypeDescriptor _descriptor;
    private readonly Func<ITypeDefinition, string> _createName;

    public UnionTypeNameDependencyDescriptor(
        IUnionTypeDescriptor descriptor,
        Func<ITypeDefinition, string> createName)
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

    public IUnionTypeDescriptor DependsOn(TypeReference typeReference)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeReference);
        return _descriptor;
    }
}
