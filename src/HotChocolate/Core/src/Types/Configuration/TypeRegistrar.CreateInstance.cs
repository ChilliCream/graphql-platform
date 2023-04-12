using System;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed partial class TypeRegistrar
{
    public TypeSystemObjectBase CreateInstance(Type namedSchemaType)
    {
        try
        {
            var constructor = ActivatorHelper.ResolveConstructor(namedSchemaType);
            var parameters = constructor.GetParameters();

            if(parameters.Length == 0)
            {
                return (TypeSystemObjectBase)constructor.Invoke(Array.Empty<object>());
            }

            var args = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                // we first try to resolve the service from the schema service provider.
                var service = _schemaServices.GetService(parameter.ParameterType);

                // if we cannot do that and there is an application level service provider
                // we will try to resolve it from there.
                if(service is null && _applicationServices is not null)
                {
                    service = _applicationServices.GetService(parameter.ParameterType);
                }

                args[i] = service;
            }

            return (TypeSystemObjectBase)constructor.Invoke(args);
        }
        catch (Exception ex)
        {
            throw TypeRegistrar_CreateInstanceFailed(namedSchemaType, ex);
        }
    }
}
