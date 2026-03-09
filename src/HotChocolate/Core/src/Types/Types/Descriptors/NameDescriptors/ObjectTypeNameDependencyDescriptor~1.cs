using HotChocolate.Types.Helpers;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors;

internal sealed class ObjectTypeNameDependencyDescriptor<T>
    : IObjectTypeNameDependencyDescriptor<T>
{
    private readonly IObjectTypeDescriptor<T> _descriptor;
    private readonly Func<ITypeDefinition, string> _createName;

    public ObjectTypeNameDependencyDescriptor(
        IObjectTypeDescriptor<T> descriptor,
        Func<ITypeDefinition, string> createName)
    {
        _descriptor = descriptor
            ?? throw new ArgumentNullException(nameof(descriptor));
        _createName = createName
            ?? throw new ArgumentNullException(nameof(createName));
    }

    public IObjectTypeDescriptor<T> DependsOn<TDependency>()
        where TDependency : IType
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeof(TDependency));
        return _descriptor;
    }

    public IObjectTypeDescriptor<T> DependsOn(Type schemaType)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, schemaType);
        return _descriptor;
    }

    public IObjectTypeDescriptor<T> DependsOn(TypeReference typeReference)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeReference);
        return _descriptor;
    }
}
