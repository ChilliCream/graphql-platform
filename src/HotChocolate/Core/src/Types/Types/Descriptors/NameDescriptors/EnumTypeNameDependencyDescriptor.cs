using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

internal class EnumTypeNameDependencyDescriptor
    : IEnumTypeNameDependencyDescriptor
{
    private readonly IEnumTypeDescriptor _descriptor;
    private readonly Func<INamedType, string> _createName;

    public EnumTypeNameDependencyDescriptor(
        IEnumTypeDescriptor descriptor,
        Func<INamedType, string> createName)
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
}
