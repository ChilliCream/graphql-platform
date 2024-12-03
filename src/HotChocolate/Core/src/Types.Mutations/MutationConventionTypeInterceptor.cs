using HotChocolate.Types.Helpers;
using static HotChocolate.WellKnownMiddleware;
using static HotChocolate.Types.Descriptors.TypeReference;
using static HotChocolate.Resolvers.FieldClassMiddlewareFactory;
using static HotChocolate.Types.Descriptors.Definitions.TypeDependencyFulfilled;
using static HotChocolate.Types.ErrorContextDataKeys;
using static HotChocolate.Types.ThrowHelper;
using static HotChocolate.Utilities.ThrowHelper;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Types;

internal sealed class MutationConventionTypeInterceptor : TypeInterceptor
{
    private readonly ErrorTypeHelper _errorTypeHelper = new();
    private TypeInitializer _typeInitializer = default!;
    private TypeRegistry _typeRegistry = default!;
    private TypeLookup _typeLookup = default!;
    private IDescriptorContext _context = default!;
    private List<MutationContextData> _mutations = default!;
    private ITypeCompletionContext _completionContext = default!;
    private ObjectTypeDefinition? _mutationTypeDef;
    private FieldMiddlewareDefinition? _errorNullMiddleware;
    private TypeInterceptor[] _siblings = [];

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
        _errorTypeHelper.InitializerErrorTypeInterface(_context);
    }

    internal override bool IsMutationAggregator(IDescriptorContext context) => true;

    internal override void SetSiblings(TypeInterceptor[] all)
    {
        if (all.Length == 1)
        {
            return;
        }

        _siblings = new TypeInterceptor[all.Length - 1];
        var j = 0;

        for (var i = 0; i < all.Length; i++)
        {
            var interceptor = all[i];

            if (!ReferenceEquals(interceptor, this))
            {
                _siblings[j++] = interceptor;
            }
        }
    }

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        // we will use the error interface and implement it with each error type.
        if (definition is ObjectTypeDefinition objectTypeDef)
        {
            TryAddErrorInterface(objectTypeDef, _errorTypeHelper.ErrorTypeInterfaceRef);
        }
    }

    public override void OnAfterCompleteTypeNames()
        => _mutations = _context.ContextData.GetMutationFields();

    internal override void OnBeforeCompleteMutation(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition)
    {
        // we first invoke our siblings to gather configurations.
        foreach (var sibling in _siblings)
        {
            sibling.OnBeforeCompleteMutation(completionContext, definition);
        }

        // we need to capture a completion context to resolve types.
        // any context will do.
        _completionContext ??= completionContext;
        _mutationTypeDef = definition;

        // if we have found a mutation type we will start applying the mutation conventions
        // on the mutations.
        if (_mutationTypeDef is not null)
        {
            HashSet<MutationContextData> unprocessed = [.._mutations,];
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
                    TryApplyInputConvention(_context.ResolverCompiler, mutationField, mutationOptions);
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

                if (context.Result is IFieldResult result)
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
                        context.Result = ErrorMarker.Instance;
                    }
                }

                // we will replace null with our null marker object so
                // that
                context.Result ??= NullMarker.Instance;
            },
            isRepeatable: false,
            key: MutationResult);

        mutation.MiddlewareDefinitions.Insert(0, middlewareDef);
    }

    private void TryApplyInputConvention(
        IResolverCompiler resolverCompiler,
        ObjectFieldDefinition mutation,
        Options options)
    {
        if (mutation.Arguments.Count is 0)
        {
            return;
        }

        if (mutation.Member is not null)
        {
            var argumentNameMap = TypeMemHelper.RentArgumentNameMap();

            foreach (var arg in mutation.Arguments)
            {
                if (arg.Parameter is not null)
                {
                    argumentNameMap.Add(arg.Parameter, arg.Name);
                }
            }

            mutation.Resolvers =
                resolverCompiler.CompileResolve(
                    mutation.Member,
                    mutation.SourceType,
                    mutation.ResolverType,
                    argumentNameMap);

            TypeMemHelper.Return(argumentNameMap);
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

            var argumentType = _completionContext.GetType<IInputType>(argument.Type!);

            var formatter =
                argument.Formatters.Count switch
                {
                    0 => null,
                    1 => argument.Formatters[0],
                    _ => new AggregateInputValueFormatter(argument.Formatters),
                };

            var defaultValue = argument.DefaultValue;

            if(defaultValue is null && argument.RuntimeDefaultValue is not null)
            {
                defaultValue =
                    _context.InputFormatter.FormatValue(
                        argument.RuntimeDefaultValue,
                        argumentType,
                        Path.Root);
            }

            resolverArguments.Add(
                new ResolverArgument(
                    argument.Name,
                    new SchemaCoordinate(inputTypeName, memberName: argument.Name),
                    _completionContext.GetType<IInputType>(argument.Type!),
                    runtimeType,
                    defaultValue,
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
        var errorDefinitions = _errorTypeHelper.GetErrorDefinitions(mutation);
        FieldDef? errorField = null;
        var errorInterfaceIsRegistered = false;
        var errorInterfaceTypeRef = _errorTypeHelper.ErrorTypeInterfaceRef;

        foreach (var errorDef in errorDefinitions)
        {
            var obj = TryRegisterType(errorDef.SchemaType);
            if (obj is not null)
            {
                var errorTypeRef = Create(obj.Type);
                _typeRegistry.Register(obj);
                _typeRegistry.TryRegister(
                    _context.TypeInspector.GetOutputTypeRef(errorDef.SchemaType),
                    errorTypeRef);
                _typeRegistry.TryRegister(
                    _context.TypeInspector.GetOutputTypeRef(errorDef.RuntimeType),
                    errorTypeRef);
                ((ObjectType)obj.Type).Definition!.Interfaces.Add(errorInterfaceTypeRef);
            }

            if (!errorInterfaceIsRegistered && _typeRegistry.TryGetTypeRef(errorInterfaceTypeRef, out _))
            {
                continue;
            }

            var err = TryRegisterType(errorInterfaceTypeRef.Type.Type);
            if (err is not null)
            {
                err.References.Add(errorInterfaceTypeRef);
                _typeRegistry.Register(err);
                _typeRegistry.TryRegister(errorInterfaceTypeRef, Create(err.Type));
            }
            errorInterfaceIsRegistered = true;
        }

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
                if (resultField.Type is null
                    || (resultField.Flags & FieldFlags.MutationQueryField) == FieldFlags.MutationQueryField)
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
                    pureResolver: ctx =>
                    {
                        ctx.ScopedContextData.TryGetValue(Errors, out var errors);
                        return errors;
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
            new(payloadFieldName, EnsureNullable(NormalizeTypeRef(mutation.Type!))),
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

                if (ReferenceEquals(ErrorMarker.Instance, parent) ||
                    ReferenceEquals(NullMarker.Instance, parent))
                {
                    return null;
                }

                return parent;
            });
        objectDef.ContextData.Add(MutationConventionDataField, dataFieldDef.Name);
        objectDef.Fields.Add(dataFieldDef);

        // if the mutation has domain errors we will add the errors
        // field to the payload type.
        if (error is not null)
        {
            var errorFieldDef = new ObjectFieldDefinition(
                error.Value.Name,
                type: error.Value.Type,
                pureResolver: ctx =>
                {
                    ctx.ScopedContextData.TryGetValue(Errors, out var errors);
                    return errors;
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

    private static void TryAddErrorInterface(
        ObjectTypeDefinition objectTypeDef,
        TypeReference errorInterfaceTypeRef)
    {
        if (objectTypeDef.ContextData.IsError())
        {
            objectTypeDef.Interfaces.Add(errorInterfaceTypeRef);
        }
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

    private RegisteredType? TryRegisterType(Type type)
    {
        if (_typeRegistry.IsRegistered(_context.TypeInspector.GetOutputTypeRef(type)))
        {
            return null;
        }

        var registeredType = _typeInitializer.InitializeType(type);
        _typeInitializer.CompleteTypeName(registeredType);
        _typeInitializer.CompileResolvers(registeredType);

        if (registeredType.Type is ObjectType errorObject &&
            errorObject.RuntimeType != typeof(object))
        {
            foreach (var possibleInterface in _typeRegistry.Types)
            {
                if (possibleInterface.Type is InterfaceType interfaceType &&
                    interfaceType.RuntimeType != typeof(object) &&
                    interfaceType.RuntimeType.IsAssignableFrom(errorObject.RuntimeType))
                {
                    var typeRef = possibleInterface.TypeReference;
                    errorObject.Definition!.Interfaces.Add(typeRef);
                    registeredType.Dependencies.Add(new(typeRef, Completed));
                }
                else if (possibleInterface.Type is UnionType unionType &&
                    unionType.RuntimeType != typeof(object) &&
                    unionType.RuntimeType.IsAssignableFrom(errorObject.RuntimeType))
                {
                    var typeRef = registeredType.TypeReference;
                    unionType.Definition!.Types.Add(typeRef);
                    possibleInterface.Dependencies.Add(new(typeRef, Completed));
                }
            }
        }
        else if (registeredType.Type is ObjectType errorInterface &&
            errorInterface.RuntimeType != typeof(object))
        {
            foreach (var possibleInterface in _typeRegistry.Types)
            {
                if (possibleInterface.Type is InterfaceType interfaceType &&
                    interfaceType.RuntimeType != typeof(object) &&
                    interfaceType.RuntimeType.IsAssignableFrom(errorInterface.RuntimeType))
                {
                    var typeRef = possibleInterface.TypeReference;
                    errorInterface.Definition!.Interfaces.Add(typeRef);
                    registeredType.Dependencies.Add(new(typeRef, Completed));
                }
            }
        }

        return registeredType;
    }

    private void RegisterType(TypeSystemObjectBase type)
    {
        var registeredType = _typeInitializer.InitializeType(type);
        _typeInitializer.CompleteTypeName(registeredType);
        _typeInitializer.CompileResolvers(registeredType);
    }

    private void RegisterErrorType(
        TypeSystemObjectBase type,
        string mutationName)
    {
        try
        {
            var registeredType = _typeInitializer.InitializeType(type);
            _typeInitializer.CompleteTypeName(registeredType);
            _typeInitializer.CompileResolvers(registeredType);
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

    private static ITypeNode CreateTypeNode(IType type)
        => type switch
        {
            NonNullType nnt => new NonNullTypeNode((INullableTypeNode) CreateTypeNode(nnt.Type)),
            ListType lt => new ListTypeNode(CreateTypeNode(lt.ElementType)),
            INamedType nt => new NamedTypeNode(nt.Name),
            _ => throw new NotSupportedException("Type is not supported."),
        };

    private static TypeReference NormalizeTypeRef(TypeReference typeRef)
    {
        if (typeRef is ExtendedTypeReference { Type.IsGeneric: true, } extendedTypeRef &&
            typeof(IFieldResult).IsAssignableFrom(extendedTypeRef.Type.Type))
        {
            return extendedTypeRef.WithType(extendedTypeRef.Type.TypeArguments[0]);
        }

        return typeRef;
    }

    private readonly ref struct Options(
        string? inputTypeNamePattern,
        string? inputArgumentName,
        string? payloadTypeNamePattern,
        string? payloadErrorTypeNamePattern,
        string? payloadErrorsFieldName,
        bool? apply)
    {
        public string InputTypeNamePattern { get; } = inputTypeNamePattern ??
            MutationConventionOptionDefaults.InputTypeNamePattern;

        public string InputArgumentName { get; } = inputArgumentName ??
            MutationConventionOptionDefaults.InputArgumentName;

        public string PayloadTypeNamePattern { get; } = payloadTypeNamePattern ??
            MutationConventionOptionDefaults.PayloadTypeNamePattern;

        public string PayloadErrorTypeNamePattern { get; } = payloadErrorTypeNamePattern ??
            MutationConventionOptionDefaults.ErrorTypeNamePattern;

        public string PayloadErrorsFieldName { get; } = payloadErrorsFieldName ??
            MutationConventionOptionDefaults.PayloadErrorsFieldName;

        public bool Apply { get; } = apply ??
            MutationConventionOptionDefaults.ApplyToAllMutations;

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

    private readonly struct FieldDef(string name, TypeReference type)
    {
        public string Name { get; } = name;

        public TypeReference Type { get; } = type;
    }
}
