using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

internal class EnumTypeNameDependencyDescriptor<T>
    : IEnumTypeNameDependencyDescriptor<T>
{
    private readonly IEnumTypeDescriptor<T> _descriptor;
    private readonly Func<INamedType, string> _createName;

    public EnumTypeNameDependencyDescriptor(
        IEnumTypeDescriptor<T> descriptor,
        Func<INamedType, string> createName)
    {
        _descriptor = descriptor
            ?? throw new ArgumentNullException(nameof(descriptor));
        _createName = createName
            ?? throw new ArgumentNullException(nameof(createName));
    }

    public IEnumTypeDescriptor<T> DependsOn<TDependency>()
        where TDependency : IType
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeof(TDependency));
        return _descriptor;
    }

    public IEnumTypeDescriptor<T> DependsOn(Type schemaType)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, schemaType);
        return _descriptor;
    }
}
