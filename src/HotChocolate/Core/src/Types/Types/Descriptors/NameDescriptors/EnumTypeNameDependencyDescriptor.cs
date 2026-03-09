using HotChocolate.Types.Helpers;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors;

internal sealed class EnumTypeNameDependencyDescriptor
    : IEnumTypeNameDependencyDescriptor
{
    private readonly IEnumTypeDescriptor _descriptor;
    private readonly Func<ITypeDefinition, string> _createName;

    public EnumTypeNameDependencyDescriptor(
        IEnumTypeDescriptor descriptor,
        Func<ITypeDefinition, string> createName)
    {
        _descriptor = descriptor
            ?? throw new ArgumentNullException(nameof(descriptor));
        _createName = createName
            ?? throw new ArgumentNullException(nameof(createName));
    }

    public IEnumTypeDescriptor DependsOn<TDependency>()
        where TDependency : IType
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeof(TDependency));
        return _descriptor;
    }

    public IEnumTypeDescriptor DependsOn(Type schemaType)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, schemaType);
        return _descriptor;
    }

    public IEnumTypeDescriptor DependsOn(TypeReference typeReference)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeReference);
        return _descriptor;
    }
}
