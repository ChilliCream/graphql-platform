using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed record RuntimeTypeBinding(Type RuntimeType, Type SchemaType, TypeContext Context)
{
    public ExtendedTypeReference GetRuntimeTypeReference(ITypeInspector typeInspector)
        => typeInspector.GetTypeRef(RuntimeType, Context);

    public ExtendedTypeReference GetSchemaTypeReference(ITypeInspector typeInspector)
        => typeInspector.GetTypeRef(SchemaType, Context);
}
