using HotChocolate.Types;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Configuration;

internal sealed partial class TypeRegistrar
{
    public TypeSystemObject CreateInstance(Type namedSchemaType)
    {
        try
        {
            return (TypeSystemObject)ActivatorUtilities.CreateInstance(_combinedServices, namedSchemaType);
        }
        catch (Exception ex)
        {
            throw TypeRegistrar_CreateInstanceFailed(namedSchemaType, ex);
        }
    }
}
