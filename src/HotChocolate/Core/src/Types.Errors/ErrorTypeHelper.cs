using static HotChocolate.Types.ErrorContextDataKeys;

namespace HotChocolate.Types;

internal sealed class ErrorTypeHelper
{
    private readonly HashSet<Type> _handled = [];
    private readonly List<ErrorDefinition> _tempErrors = [];
    private ExtendedTypeReference? _errorInterfaceTypeRef;

    public ExtendedTypeReference ErrorTypeInterfaceRef
    {
        get
        {
            if (_errorInterfaceTypeRef is null)
            {
                throw new InvalidOperationException("ErrorTypeHelper is not initialized.");
            }

            return _errorInterfaceTypeRef;
        }
    }

    public IReadOnlyList<ErrorDefinition> GetErrorDefinitions(
        ObjectFieldDefinition field)
    {
        var errorTypes = GetErrorResultTypes(field);

        if (field.ContextData.TryGetValue(ErrorDefinitions, out var value) &&
            value is IReadOnlyList<ErrorDefinition> errorDefs)
        {
            if (errorTypes.Length == 0)
            {
                return errorDefs;
            }

            _handled.Clear();
            _tempErrors.Clear();

            foreach (var errorDef in errorDefs)
            {
                _handled.Add(errorDef.RuntimeType);
                _tempErrors.Add(errorDef);
            }

            CreateErrorDefinitions(errorTypes, _handled, _tempErrors);

            return _tempErrors.ToArray();
        }

        if (errorTypes.Length > 0)
        {
            _handled.Clear();
            _tempErrors.Clear();

            CreateErrorDefinitions(errorTypes, _handled, _tempErrors);

            return _tempErrors.ToArray();
        }

        return Array.Empty<ErrorDefinition>();

        // ReSharper disable once VariableHidesOuterVariable
        static void CreateErrorDefinitions(
            Type[] errorTypes,
            HashSet<Type> handled,
            List<ErrorDefinition> tempErrors)
        {
            foreach (var errorType in errorTypes)
            {
                if (!handled.Add(errorType))
                {
                    continue;
                }

                if (typeof(Exception).IsAssignableFrom(errorType))
                {
                    var schemaType = typeof(ExceptionObjectType<>).MakeGenericType(errorType);
                    var definition = new ErrorDefinition(
                        errorType,
                        schemaType,
                        ex => ex.GetType() == errorType
                            ? ex
                            : null);
                    tempErrors.Add(definition);
                }
                else
                {
                    var schemaType = typeof(ErrorObjectType<>).MakeGenericType(errorType);
                    var definition = new ErrorDefinition(errorType, schemaType, _ => null);
                    tempErrors.Add(definition);
                }
            }
        }
    }

    private static Type[] GetErrorResultTypes(ObjectFieldDefinition mutation)
    {
        var resultType = mutation.ResultType;

        if (resultType?.IsGenericType ?? false)
        {
            var typeDefinition = resultType.GetGenericTypeDefinition();

            if (typeDefinition == typeof(Task<>) || typeDefinition == typeof(ValueTask<>))
            {
                resultType = resultType.GenericTypeArguments[0];
            }
        }

        if (resultType is { IsValueType: true, IsGenericType: true, } &&
            typeof(IFieldResult).IsAssignableFrom(resultType))
        {
            var types = resultType.GenericTypeArguments;

            if (types.Length > 1)
            {
                var errorTypes = new Type[types.Length - 1];

                for (var i = 1; i < types.Length; i++)
                {
                    errorTypes[i - 1] = types[i];
                }

                return errorTypes;
            }
        }

        return [];
    }

    public void InitializerErrorTypeInterface(IDescriptorContext context)
    {
        if (_errorInterfaceTypeRef is not null)
        {
            return;
        }

        var key = typeof(ErrorInterfaceType).FullName!;

        if (!context.ContextData.TryGetValue(key, out var value))
        {
            value = CreateErrorTypeRef(context);
            context.ContextData.Add(key, value);
        }

        _errorInterfaceTypeRef = (ExtendedTypeReference)value!;
    }

    private static ExtendedTypeReference CreateErrorTypeRef(IDescriptorContext context)
    {
        var errorInterfaceType =
            context.ContextData.TryGetValue(ErrorType, out var value) &&
            value is Type type
                ? type
                : typeof(ErrorInterfaceType);

        if (!context.TypeInspector.IsSchemaType(errorInterfaceType))
        {
            errorInterfaceType = typeof(InterfaceType<>).MakeGenericType(errorInterfaceType);
        }

        return context.TypeInspector.GetOutputTypeRef(errorInterfaceType);
    }
}
