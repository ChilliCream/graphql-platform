using System.Linq;
using static HotChocolate.WellKnownMiddleware;
using static HotChocolate.Types.Descriptors.TypeReference;
using static HotChocolate.Resolvers.FieldClassMiddlewareFactory;
using static HotChocolate.Types.ErrorContextDataKeys;

#nullable enable

namespace HotChocolate.Types;

internal sealed class MutationConventionTypeInterceptor : TypeInterceptor
{
    private readonly HashSet<Type> _handled = new();
    private readonly List<ErrorDefinition> _tempErrors = new();
    private TypeInitializer _typeInitializer = default!;
    private TypeRegistry _typeRegistry = default!;
    private TypeLookup _typeLookup = default!;
    private IDescriptorContext _context = default!;
    private List<MutationContextData> _mutations = default!;
    private ITypeCompletionContext _completionContext = default!;
    private ITypeReference? _errorInterfaceTypeRef;
    private ObjectTypeDefinition? _mutationTypeDef;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _context = context;
        _typeInitializer = typeInitializer;
        _typeRegistry = typeRegistry;
        _typeLookup = typeLookup;
    }

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        // first we need to get a handle on the error interface.
        _errorInterfaceTypeRef ??= CreateErrorTypeRef(discoveryContext);

        // we will use the error interface and implement it with each error type.
        if (definition is ObjectTypeDefinition objectTypeDef)
        {
            TryAddErrorInterface(objectTypeDef, _errorInterfaceTypeRef);
        }
    }

    public override void OnAfterCompleteTypeNames()
        => _mutations = _context.ContextData.GetMutationFields();

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        OperationType operationType)
    {
        // if the type initialization resolved the root type we will capture its definition
        // so we can use it after the type extensions are merged into this type.
        if (operationType is OperationType.Mutation)
        {
            _mutationTypeDef = (ObjectTypeDefinition)definition!;
        }

        // we need to capture a completion context to resolve types.
        // any context will do.
        _completionContext ??= completionContext;
    }

    public override void OnAfterMergeTypeExtensions()
    {
        // if we have found a mutation type we will start applying the mutation conventions
        // on the mutations.
        if (_mutationTypeDef is not null)
        {
            HashSet<MutationContextData> unprocessed = new(_mutations);
            var defLookup = _mutations.ToDictionary(t => t.Definition);
            var nameLookup = _mutations.ToDictionary(t => t.Name);
            var rootOptions = CreateOptions(_context.ContextData);

            foreach (var mutationField in _mutationTypeDef.Fields)
            {
                if (mutationField.IsIntrospectionField)
                {
                    // we skip over any introspection fields like `__typename`
                    continue;
                }

                var mutationOptions = rootOptions;

                // if the mutation has any error attributes we will interpret that as an opt-in
                // to the mutation conventions.
                if (mutationField.ContextData.ContainsKey(ErrorDefinitions) &&
                    !mutationOptions.Apply)
                {
                    mutationOptions = CreateErrorOptions(mutationOptions);
                }

                // next we check for specific mutation configuration overrides.
                // if a user provided specific mutation settings they will take
                // precedence over global and inferred settings.
                if (defLookup.TryGetValue(mutationField, out var cd) ||
                    nameLookup.TryGetValue(mutationField.Name, out cd))
                {
                    mutationOptions = CreateOptions(cd, mutationOptions);
                    unprocessed.Remove(cd);
                }

                if (mutationOptions.Apply)
                {
                    // if the mutation options indicate that we shall apply the mutation
                    // conventions we will start rewriting the field.
                    TryApplyInputConvention(mutationField, mutationOptions);
                    TryApplyPayloadConvention(mutationField, cd?.PayloadFieldName, mutationOptions);
                }
            }

            if (unprocessed.Count > 0)
            {
                throw ThrowHelper.NonMutationFields(unprocessed);
            }
        }
    }

    private void TryApplyInputConvention(ObjectFieldDefinition mutation, Options options)
    {
        if (mutation.Arguments.Count is 0)
        {
            return;
        }

        var inputTypeName = options.FormatInputTypeName(mutation.Name);

        if (_typeRegistry.NameRefs.ContainsKey(inputTypeName))
        {
            return;
        }

        var inputType = CreateInputType(inputTypeName, mutation);
        RegisterType(inputType);

        var resolverArguments = new List<ResolverArgument>();

        foreach (var argument in mutation.Arguments)
        {
            var runtimeType = argument.RuntimeType ??
                argument.Parameter?.ParameterType ??
                typeof(object);

            var formatter =
                argument.Formatters.Count switch
                {
                    0 => null,
                    1 => argument.Formatters[0],
                    _ => new AggregateInputValueFormatter(argument.Formatters)
                };

            resolverArguments.Add(
                new ResolverArgument(
                    argument.Name,
                    new FieldCoordinate(inputTypeName, argument.Name),
                    _completionContext.GetType<IInputType>(argument.Type!),
                    runtimeType,
                    argument.DefaultValue,
                    formatter));
        }

        var argumentMiddleware =
            new FieldMiddlewareDefinition(
                Create<MutationConventionMiddleware>(
                    (typeof(string), options.InputArgumentName),
                    (typeof(IReadOnlyList<ResolverArgument>), resolverArguments)),
                key: MutationArguments,
                isRepeatable: false);

        mutation.Arguments.Clear();
        mutation.Arguments.Add(new(options.InputArgumentName, type: Parse($"{inputTypeName}!")));
        mutation.MiddlewareDefinitions.Insert(0, argumentMiddleware);
    }

    private void TryApplyPayloadConvention(
        ObjectFieldDefinition mutation,
        string? payloadFieldName,
        Options options)
    {
        var typeRef = mutation.Type;
        var payloadTypeName = options.FormatPayloadTypeName(mutation.Name);

        // we ensure that we can resolve the mutation result type.
        if (!_typeLookup.TryNormalizeReference(typeRef!, out typeRef) ||
            !_typeRegistry.TryGetType(typeRef, out var registration))
        {
            throw ThrowHelper.CannotResolvePayloadType();
        }

        // if the mutation result type matches the payload type name pattern
        // we expect it to be already a proper payload and will not transform the
        // result type to a payload.
        if (registration.Type.Name.EqualsOrdinal(payloadTypeName))
        {
            return;
        }

        // before starting to build the payload type we first will look for error definitions
        // an the mutation.
        var errorDefinitions = GetErrorDefinitions(mutation);
        FieldDef? errorField = null;

        // if this mutation has error definitions we will create the error middleware,
        // the payload error type and the error field definition that will be exposed
        // on the payload.
        if (errorDefinitions.Count > 0)
        {
            // create error type
            var errorTypeName = options.FormatErrorTypeName(mutation.Name);
            RegisterType(CreateErrorType(errorTypeName, errorDefinitions));
            var errorListTypeRef = Parse($"[{errorTypeName}!]");
            errorField = new FieldDef(options.PayloadErrorsFieldName, errorListTypeRef);

            // collect error factories for middleware
            var errorFactories = errorDefinitions.Select(t => t.Factory).ToArray();

            // create middleware
            var errorMiddleware =
                new FieldMiddlewareDefinition(
                    Create<ErrorMiddleware>(
                        (typeof(IReadOnlyList<CreateError>), errorFactories)),
                    key: MutationErrors,
                    isRepeatable: false);

            mutation.MiddlewareDefinitions.Insert(0, errorMiddleware);
        }

        // lastly we will create the mutation payload and replace with it the current mutation
        // result type.
        payloadFieldName ??= _context.Naming.FormatFieldName(registration.Type.Name);

        var type = CreatePayloadType(
            payloadTypeName,
            new(payloadFieldName, EnsureNullable(mutation.Type!)),
            errorField);
        RegisterType(type);

        mutation.Type = Parse($"{payloadTypeName}!");

        // we mustn't forget to drop the error definitions at this point since we do not
        // want to preserve them on the actual schema field.
        mutation.ContextData.Remove(ErrorDefinitions);
    }

    private static InputObjectType CreateInputType(
        string typeName,
        ObjectFieldDefinition fieldDef)
    {
        var inputObjectDef = new InputObjectTypeDefinition(typeName);

        foreach (var argumentDef in fieldDef.Arguments)
        {
            var inputFieldDef = new InputFieldDefinition();
            argumentDef.CopyTo(inputFieldDef);

            inputFieldDef.RuntimeType =
                argumentDef.RuntimeType ??
                argumentDef.Parameter?.ParameterType;

            inputObjectDef.Fields.Add(inputFieldDef);
        }

        return InputObjectType.CreateUnsafe(inputObjectDef);
    }

    private static ObjectType CreatePayloadType(
        string typeName,
        FieldDef data,
        FieldDef? error)
    {
        var objectDef = new ObjectTypeDefinition(typeName);

        var dataFieldDef = new ObjectFieldDefinition(
            data.Name,
            type: data.Type,
            pureResolver: ctx =>
            {
                var parent = ctx.Parent<object?>();

                if (ReferenceEquals(MarkerObjects.ErrorObject, parent) ||
                    ReferenceEquals(MarkerObjects.Null, parent))
                {
                    return null;
                }

                return parent;
            });
        objectDef.Fields.Add(dataFieldDef);

        // if the mutation has domain errors we will add the errors
        // field to the payload type.
        if (error is not null)
        {
            var errorFieldDef = new ObjectFieldDefinition(
                error.Value.Name,
                type: error.Value.Type,
                resolver: ctx =>
                {
                    ctx.ScopedContextData.TryGetValue(Errors, out var errors);
                    return new ValueTask<object?>(errors);
                });
            objectDef.Fields.Add(errorFieldDef);
        }

        return ObjectType.CreateUnsafe(objectDef);
    }

    private UnionType CreateErrorType(
        string typeName,
        IReadOnlyList<ErrorDefinition> errorDefinitions)
    {
        var unionDef = new UnionTypeDefinition(typeName);

        foreach (var error in errorDefinitions)
        {
            unionDef.Types.Add(_context.TypeInspector.GetOutputTypeRef(error.SchemaType));
        }

        return UnionType.CreateUnsafe(unionDef);
    }

    private static ITypeReference CreateErrorTypeRef(ITypeDiscoveryContext context)
        => CreateErrorTypeRef(context.DescriptorContext);

    private static ITypeReference CreateErrorTypeRef(IDescriptorContext context)
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

    private static void TryAddErrorInterface(
        ObjectTypeDefinition objectTypeDef,
        ITypeReference errorInterfaceTypeRef)
    {
        if (objectTypeDef.ContextData.IsError())
        {
            objectTypeDef.Interfaces.Add(errorInterfaceTypeRef);
        }
    }

    private IReadOnlyList<ErrorDefinition> GetErrorDefinitions(
        ObjectFieldDefinition mutation)
    {
        var errorTypes = GetErrorResultTypes(mutation);

        if (mutation.ContextData.TryGetValue(ErrorDefinitions, out var value) &&
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
                if (handled.Add(errorType))
                {
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
                        var definition = new ErrorDefinition(
                            errorType,
                            schemaType,
                            obj => obj);
                        tempErrors.Add(definition);
                    }
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


        if (resultType is { IsValueType: true, IsGenericType: true } &&
            typeof(IMutationResult).IsAssignableFrom(resultType))
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

        return Array.Empty<Type>();
    }

    private static Options CreateOptions(
        IDictionary<string, object?> contextData)
    {
        if (contextData.TryGetValue(MutationContextDataKeys.Options, out var value) &&
            value is MutationConventionOptions options)
        {
            return new Options(
                options.InputTypeNamePattern,
                options.InputArgumentName,
                options.PayloadTypeNamePattern,
                options.PayloadErrorTypeNamePattern,
                options.PayloadErrorsFieldName,
                options.ApplyToAllMutations);
        }

        return new Options(null, null, null, null, null, null);
    }

    private static Options CreateOptions(
        MutationContextData contextData,
        Options parent = default)
    {
        return new Options(
            contextData.InputTypeName ?? parent.InputTypeNamePattern,
            contextData.InputArgumentName ?? parent.InputArgumentName,
            contextData.PayloadTypeName ?? parent.PayloadTypeNamePattern,
            contextData.PayloadPayloadErrorTypeName ?? parent.PayloadErrorTypeNamePattern,
            contextData.PayloadErrorsFieldName ?? parent.PayloadErrorsFieldName,
            contextData.Enabled);
    }

    private static Options CreateErrorOptions(
        Options parent = default)
    {
        return new Options(
            parent.InputTypeNamePattern,
            parent.InputArgumentName,
            parent.PayloadTypeNamePattern,
            parent.PayloadErrorTypeNamePattern,
            parent.PayloadErrorsFieldName,
            true);
    }

    private void RegisterType(TypeSystemObjectBase type)
    {
        var registeredType = _typeInitializer.InitializeType(type);
        _typeInitializer.CompleteTypeName(registeredType);
    }

    private ITypeReference EnsureNullable(ITypeReference typeRef)
    {
        var type = _completionContext.GetType<IType>(typeRef);

        if (type is not NonNullType nt)
        {
            return typeRef;
        }

        return Create(CreateTypeNode(nt.Type));
    }

    private ITypeNode CreateTypeNode(IType type)
        => type switch
        {
            NonNullType nnt => new NonNullTypeNode((INullableTypeNode)CreateTypeNode(nnt.Type)),
            ListType lt => new ListTypeNode(CreateTypeNode(lt.ElementType)),
            INamedType nt => new NamedTypeNode(nt.Name),
            _ => throw new NotSupportedException("Type is not supported.")
        };

    private readonly ref struct Options
    {
        public Options(
            string? inputTypeNamePattern,
            string? inputArgumentName,
            string? payloadTypeNamePattern,
            string? payloadErrorTypeNamePattern,
            string? payloadErrorsFieldName,
            bool? apply)
        {
            InputTypeNamePattern = inputTypeNamePattern ??
                MutationConventionOptionDefaults.InputTypeNamePattern;
            InputArgumentName = inputArgumentName ??
                MutationConventionOptionDefaults.InputArgumentName;
            PayloadTypeNamePattern = payloadTypeNamePattern ??
                MutationConventionOptionDefaults.PayloadTypeNamePattern;
            PayloadErrorsFieldName = payloadErrorsFieldName ??
                MutationConventionOptionDefaults.PayloadErrorsFieldName;
            PayloadErrorTypeNamePattern = payloadErrorTypeNamePattern ??
                MutationConventionOptionDefaults.ErrorTypeNamePattern;
            Apply = apply ??
                MutationConventionOptionDefaults.ApplyToAllMutations;
        }

        public string InputTypeNamePattern { get; }

        public string InputArgumentName { get; }

        public string PayloadTypeNamePattern { get; }

        public string PayloadErrorTypeNamePattern { get; }

        public string PayloadErrorsFieldName { get; }

        public bool Apply { get; }

        public string FormatInputTypeName(string mutationName)
            => InputTypeNamePattern.Replace(
                $"{{{MutationConventionOptionDefaults.MutationName}}}",
                char.ToUpper(mutationName[0]) + mutationName.Substring(1));

        public string FormatPayloadTypeName(string mutationName)
            => PayloadTypeNamePattern.Replace(
                $"{{{MutationConventionOptionDefaults.MutationName}}}",
                char.ToUpper(mutationName[0]) + mutationName.Substring(1));

        public string FormatErrorTypeName(string mutationName)
            => PayloadErrorTypeNamePattern.Replace(
                $"{{{MutationConventionOptionDefaults.MutationName}}}",
                char.ToUpper(mutationName[0]) + mutationName.Substring(1));
    }

    private readonly struct FieldDef
    {
        public FieldDef(string name, ITypeReference type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public ITypeReference Type { get; }
    }
}
