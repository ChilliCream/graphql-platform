#nullable enable
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class ExtendedTypeDirectiveReferenceHandler : ITypeRegistrarHandler
{
    private readonly ITypeInspector _typeInspector;

    public ExtendedTypeDirectiveReferenceHandler(ITypeInspector typeInspector)
        => _typeInspector = typeInspector;

    public TypeReferenceKind Kind => TypeReferenceKind.DirectiveExtendedType;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (ExtendedTypeDirectiveReference)typeReference;

        if (_typeInspector.TryCreateTypeInfo(typeRef.Type, out var typeInfo) &&
            !ExtendedType.Tools.IsSchemaType(typeInfo.NamedType) &&
            !typeRegistrar.IsResolved(typeRef))
        {
            typeRegistrar.MarkUnresolved(typeRef);
        }
    }
}
