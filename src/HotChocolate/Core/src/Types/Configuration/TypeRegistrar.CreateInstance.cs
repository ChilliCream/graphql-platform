using HotChocolate.Types;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Configuration;

internal sealed partial class TypeRegistrar
{
    public TypeSystemObject CreateInstance(Type namedSchemaType)
    {
        try
        {
            var services = _applicationServices ?? _schemaServices;

            return (TypeSystemObject)ActivatorUtilities.CreateInstance(services, namedSchemaType);
        }
        catch (Exception ex)
        {
            throw TypeRegistrar_CreateInstanceFailed(namedSchemaType, ex);
        }
    }
}
