using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

internal class InputObjectTypeNameDependencyDescriptor<T>
    : IInputObjectTypeNameDependencyDescriptor<T>
{
    private readonly IInputObjectTypeDescriptor<T> _descriptor;
    private readonly Func<INamedType, string> _createName;

    public InputObjectTypeNameDependencyDescriptor(
        IInputObjectTypeDescriptor<T> descriptor,
        Func<INamedType, string> createName)
    {
        _descriptor = descriptor
            ?? throw new ArgumentNullException(nameof(descriptor));
        _createName = createName
            ?? throw new ArgumentNullException(nameof(createName));
    }

    public IInputObjectTypeDescriptor<T> DependsOn<TDependency>()
        where TDependency : IType
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeof(TDependency));
        return _descriptor;
    }

    public IInputObjectTypeDescriptor<T> DependsOn(Type schemaType)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, schemaType);
        return _descriptor;
    }
}
