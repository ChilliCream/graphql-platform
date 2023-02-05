using System.Linq;
using static HotChocolate.WellKnownMiddleware;
using static HotChocolate.Types.Descriptors.TypeReference;
using static HotChocolate.Resolvers.FieldClassMiddlewareFactory;
using static HotChocolate.Types.ErrorContextDataKeys;
using static HotChocolate.Types.ThrowHelper;
using static HotChocolate.Utilities.ThrowHelper;
using static HotChocolate.WellKnownContextData;

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
    private TypeReference? _errorInterfaceTypeRef;
    private ObjectTypeDefinition? _mutationTypeDef;
    private FieldMiddlewareDefinition? _errorNullMiddleware;

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
        DefinitionBase definition)
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
                    ApplyResultMiddleware(mutationField);
                    TryApplyInputConvention(mutationField, mutationOptions);
                    TryApplyPayloadConvention(mutationField, cd?.PayloadFieldName, mutationOptions);
                }
            }

            if (unprocessed.Count > 0)
            {
                throw NonMutationFields(unprocessed);
            }
        }
    }

    private static void ApplyResultMiddleware(ObjectFieldDefinition mutation)
    {
        var middlewareDef = new FieldMiddlewareDefinition(
            next => async context =>
            {
                await next(context).ConfigureAwait(false);

                if (context.Result is IMutationResult result)
                {
                    // by checking if it is not an error we can accept the default
                    // value of the struct as null.
                    if (!result.IsError)
                    {
                        context.Result = result.Value;
                    }
                    else
                    {
                        context.SetScopedState(Errors, result.Value);
                        context.Result = MarkerObjects.ErrorObject;
                    }
                }

                // we will replace null with our null marker object so
                // that
                context.Result ??= MarkerObjects.Null;
            },
            isRepeatable: false,
            key: MutationResult);

        mutation.MiddlewareDefinitions.Insert(0, middlewareDef);
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
            throw CannotResolvePayloadType();
        }

        // before starting to build the payload type we first will look for error definitions
        // an the mutation.
        var errorDefinitions = GetErrorDefinitions(mutation);
        FieldDef? errorField = null;

        // if the mutation result type matches the payload type name pattern
        // we expect it to be already a proper payload and will not transform the
        // result type to a payload. However, we will check if we need to add the
        // errors field to it.
        if (registration.Type.Name.EqualsOrdinal(payloadTypeName))
        {
            if (errorDefinitions.Count <= 0)
            {
                return;
            }

            // first we retrieve the payload type that was defined by the user.
            var payloadType = _completionContext.GetType<IType>(typeRef);

            // we ensure that the payload type is an object type; otherwise we raise an error.
            if (payloadType.IsListType() || payloadType.NamedType() is not ObjectType obj)
            {
                _completionContext.ReportError(
                    MutationPayloadMustBeObject(payloadType.NamedType()));
                return;
            }

            // we grab the definition to mutate the payload type.
            var payloadTypeDef = obj.Definition!;

            // next we create a null middleware which will return null for any payload
            // field that we have on the payload type if a mutation error was returned.
            var nullMiddleware = _errorNullMiddleware ??=
                new FieldMiddlewareDefinition(
                    Create<ErrorNullMiddleware>(),
                    key: MutationErrorNull,
                    isRepeatable: false);

            foreach (var resultField in payloadTypeDef.Fields)
            {
                // if the field is the query mutation field we will allow it to stay non-nullable
                // since it does not need the parent.
                if (resultField.Type is null || resultField.CustomSettingExists(MutationQueryField))
                {
                    continue;
                }

                // first we ensure that all fields on the mutation payload are nullable.
                resultField.Type = EnsureNullable(resultField.Type);

                // next we will add the null middleware as the first middleware element
                resultField.MiddlewareDefinitions.Insert(0, nullMiddleware);
            }

            // We will ensure that the mutation return value is actually not nullable.
            // If there was an actual GraphQL error on the mutation it ensures that
            // the next mutation execution is aborted.
            //
            // mutation {
            //   currentMutationThatErrors <---
            //   nextMutationWillNotBeInvoked
            // }
            //
            // Mutations are executed sequentially and by having the payload
            // non-nullable the complete result is erased (non-null propagation)
            // which causes the execution to stop.
            mutation.Type = EnsureNonNull(mutation.Type!);

            // now that everything is put in place we will create the error types and
            // the error middleware.
            var errorTypeName = options.FormatErrorTypeName(mutation.Name);
            RegisterErrorType(CreateErrorType(errorTypeName, errorDefinitions), mutation.Name);
            var errorListTypeRef = Parse($"[{errorTypeName}!]");
            payloadTypeDef.Fields.Add(
                new ObjectFieldDefinition(
                    options.PayloadErrorsFieldName,
                    type: errorListTypeRef,
                    resolver: ctx =>
                    {
                        ctx.ScopedContextData.TryGetValue(Errors, out var errors);
                        return new ValueTask<object?>(errors);
                    }));

            // collect error factories for middleware
            var errorFactories = errorDefinitions.Select(t => t.Factory).ToArray();

            // create middleware
            var errorMiddleware =
                new FieldMiddlewareDefinition(
                    Create<ErrorMiddleware>(
                        (typeof(IReadOnlyList<CreateError>), errorFactories)),
                    key: MutationErrors,
                    isRepeatable: false);

            // last but not least we insert the error middleware to the mutation field.
            mutation.MiddlewareDefinitions.Insert(0, errorMiddleware);

            // we return here since we handled the case where the user has provided a custom
            // payload type.
            return;
        }

        // if this mutation has error definitions we will create the error middleware,
        // the payload error type and the error field definition that will be exposed
        // on the payload.
        if (errorDefinitions.Count > 0)
        {
            // create error type
            var errorTypeName = options.FormatErrorTypeName(mutation.Name);
            RegisterErrorType(CreateErrorType(errorTypeName, errorDefinitions), mutation.Name);
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

    private static TypeReference CreateErrorTypeRef(ITypeDiscoveryContext context)
        => CreateErrorTypeRef(context.DescriptorContext);

    private static TypeReference CreateErrorTypeRef(IDescriptorContext context)
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
        TypeReference errorInterfaceTypeRef)
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

    private void RegisterErrorType(
        TypeSystemObjectBase type,
        string mutationName)
    {
        try
        {
            var registeredType = _typeInitializer.InitializeType(type);
            _typeInitializer.CompleteTypeName(registeredType);
        }
        catch (SchemaException ex)
            when (ex.Errors[0].Code.EqualsOrdinal(ErrorCodes.Schema.DuplicateTypeName))
        {
            throw TypeInitializer_MutationDuplicateErrorName(
                type,
                mutationName,
                type.Name,
                ex.Errors);
        }
    }

    private TypeReference EnsureNullable(TypeReference typeRef)
    {
        var type = _completionContext.GetType<IType>(typeRef);

        if (type is not NonNullType nt)
        {
            return typeRef;
        }

        return Create(CreateTypeNode(nt.Type));
    }

    private TypeReference EnsureNonNull(TypeReference typeRef)
    {
        var type = _completionContext.GetType<IType>(typeRef);

        if (type.Kind is TypeKind.NonNull)
        {
            return typeRef;
        }

        return Create(CreateTypeNode(new NonNullType(type)));
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
        public FieldDef(string name, TypeReference type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public TypeReference Type { get; }
    }
}
