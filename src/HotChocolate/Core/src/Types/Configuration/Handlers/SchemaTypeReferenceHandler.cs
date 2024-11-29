using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class SchemaTypeReferenceHandler : ITypeRegistrarHandler
{
    public TypeReferenceKind Kind => TypeReferenceKind.SchemaType;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (SchemaTypeReference)typeReference;

        if (typeRegistrar.IsResolved(typeReference))
        {
            return;
        }

        var tsm = typeRef.Type;

        // if it is a type object we will make sure it is unwrapped.
        if (typeRef.Type is IType type)
        {
            tsm = type.NamedType();
        }

        if (tsm is TypeSystemObjectBase tso)
        {
            typeRegistrar.Register(tso, typeReference.Scope);
        }
    }
}
