using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

internal class InputObjectTypeNameDependencyDescriptor
    : IInputObjectTypeNameDependencyDescriptor
{
    private readonly IInputObjectTypeDescriptor _descriptor;
    private readonly Func<INamedType, string> _createName;

    public InputObjectTypeNameDependencyDescriptor(
        IInputObjectTypeDescriptor descriptor,
        Func<INamedType, string> createName)
    {
        _descriptor = descriptor
            ?? throw new ArgumentNullException(nameof(descriptor));
        _createName = createName
            ?? throw new ArgumentNullException(nameof(createName));
    }

    public IInputObjectTypeDescriptor DependsOn<TDependency>()
        where TDependency : IType
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeof(TDependency));
        return _descriptor;
    }

    public IInputObjectTypeDescriptor DependsOn(Type schemaType)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, schemaType);
        return _descriptor;
    }
}
