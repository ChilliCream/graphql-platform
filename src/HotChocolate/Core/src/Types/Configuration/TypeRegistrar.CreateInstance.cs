using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Configuration;

internal sealed partial class TypeRegistrar
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067",
        Justification = "Schema types are registered and preserved by the type system at runtime.")]
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
