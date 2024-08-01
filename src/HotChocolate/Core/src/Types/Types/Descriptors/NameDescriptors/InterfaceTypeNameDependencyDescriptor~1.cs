using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

internal class InterfaceTypeNameDependencyDescriptor<T>
    : IInterfaceTypeNameDependencyDescriptor<T>
{
    private readonly IInterfaceTypeDescriptor<T> _descriptor;
    private readonly Func<INamedType, string> _createName;

    public InterfaceTypeNameDependencyDescriptor(
        IInterfaceTypeDescriptor<T> descriptor,
        Func<INamedType, string> createName)
    {
        _descriptor = descriptor
            ?? throw new ArgumentNullException(nameof(descriptor));
        _createName = createName
            ?? throw new ArgumentNullException(nameof(createName));
    }

    public IInterfaceTypeDescriptor<T> DependsOn<TDependency>()
        where TDependency : IType
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeof(TDependency));
        return _descriptor;
    }

    public IInterfaceTypeDescriptor<T> DependsOn(Type schemaType)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, schemaType);
        return _descriptor;
    }
}
