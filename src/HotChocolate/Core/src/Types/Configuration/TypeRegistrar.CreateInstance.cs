using HotChocolate.Types;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed partial class TypeRegistrar
{
    public TypeSystemObjectBase CreateInstance(Type namedSchemaType)
    {
        try
        {
            return (TypeSystemObjectBase)ActivatorUtilities.CreateInstance(_combinedServices, namedSchemaType);
        }
        catch (Exception ex)
        {
            throw TypeRegistrar_CreateInstanceFailed(namedSchemaType, ex);
        }
    }
}
