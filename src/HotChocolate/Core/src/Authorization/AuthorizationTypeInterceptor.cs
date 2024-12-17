using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using static HotChocolate.Authorization.AuthorizeDirectiveType.Names;
using static HotChocolate.WellKnownContextData;
using static HotChocolate.Authorization.Properties.AuthCoreResources;

namespace HotChocolate.Authorization;

internal sealed partial class AuthorizationTypeInterceptor : TypeInterceptor
{
    private const string AspNetCoreAuthorizeAttributeName = "Microsoft.AspNetCore.Authorization.AuthorizeAttribute";
    private const string AspNetCoreAllowAnonymousAttributeName =
        "Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute";

    private static readonly string _authorizeAttributeName = typeof(AuthorizeAttribute).FullName!;
    private static readonly string _allowAnonymousAttributeName = typeof(AllowAnonymousAttribute).FullName!;

    private readonly List<ObjectTypeInfo> _objectTypes = [];
    private readonly List<UnionTypeInfo> _unionTypes = [];
    private readonly Dictionary<ObjectType, IDirectiveCollection> _directives = new();
    private readonly HashSet<TypeReference> _completedTypeRefs = [];
    private readonly HashSet<RegisteredType> _completedTypes = [];
    private State? _state;

    private IDescriptorContext _context = default!;
    private TypeInitializer _typeInitializer = default!;
    private TypeRegistry _typeRegistry = default!;
    private TypeLookup _typeLookup = default!;
    private ExtensionData _schemaContextData = default!;
    private ITypeCompletionContext _queryContext = default!;

    internal override uint Position => uint.MaxValue - 50;

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

    internal override void OnBeforeCreateSchemaInternal(
        IDescriptorContext context,
        ISchemaBuilder schemaBuilder)
    {
        // we capture the schema context data before everything else so that we can
        // set a marker if the authorization validation rules need to be executed.
        schemaBuilder.SetSchema(d => _schemaContextData = d.Extend().Definition.ContextData);
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        switch (completionContext.Type)
        {
            // at this point we collect object types so we can check if they need to be authorized.
            case ObjectType when definition is ObjectTypeDefinition objectTypeDef:
                _objectTypes.Add(new ObjectTypeInfo(completionContext, objectTypeDef));
                break;

            // also we collect union types so we can see if a union exposes
            // an authorized object type.
            case UnionType when definition is UnionTypeDefinition unionTypeDef:
                _unionTypes.Add(new UnionTypeInfo(completionContext, unionTypeDef));
                break;
        }

        // note, we do not need to collect interfaces as the object type has a
        // list implements that links to the interfaces that expose an object type.
    }

    public override void OnBeforeCompleteTypes()
    {
        // at this stage in the type initialization we will create some state that we
        // will use to transform the schema authorization.
        var state = _state = CreateState();

        // copy temporary state to schema state.
        if (_context.ContextData.TryGetValue(AuthorizationRequestPolicy, out var value))
        {
            _schemaContextData[AuthorizationRequestPolicy] = value;
        }

        // before we can apply schema transformations we will inspect the object types
        // to identify the ones that are protected with authorization directives.
        InspectObjectTypesForAuthDirective(state);

        // next we will inspect the union types that expose one or more protected object types.
        FindUnionTypesThatContainAuthTypes(state);

        // last we will find fields that expose protected types and apply authorization
        // middleware.
        FindFieldsAndApplyAuthMiddleware(state);
    }

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryContext = completionContext;
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is not ObjectTypeDefinition typeDef)
        {
            return;
        }

        // last in the initialization we need to intercept the query type and ensure that
        // authorization configuration is applied to the special introspection and node fields.
        if (ReferenceEquals(_queryContext, completionContext))
        {
            var state = _state ?? throw ThrowHelper.StateNotInitialized();
            HandleSpecialQueryFields(new ObjectTypeInfo(completionContext, typeDef), state);
        }

        if (_context.Options.ErrorOnAspNetCoreAuthorizationAttributes && !completionContext.IsIntrospectionType)
        {
            var runtimeType = typeDef.RuntimeType;
            var attributesOnType = runtimeType.GetCustomAttributes().ToArray();

            if (ContainsNamedAttribute(attributesOnType, AspNetCoreAuthorizeAttributeName))
            {
                completionContext.ReportError(
                    UnsupportedAspNetCoreAttributeError(
                        AspNetCoreAuthorizeAttributeName,
                        _authorizeAttributeName,
                        runtimeType));
                return;
            }

            if (ContainsNamedAttribute(attributesOnType, AspNetCoreAllowAnonymousAttributeName))
            {
                completionContext.ReportError(
                    UnsupportedAspNetCoreAttributeError(
                        AspNetCoreAllowAnonymousAttributeName,
                        _allowAnonymousAttributeName,
                        runtimeType));
                return;
            }

            foreach (var field in typeDef.Fields)
            {
                if (field.IsIntrospectionField)
                {
                    continue;
                }

                var fieldMember = field.ResolverMember ?? field.Member;

                if (fieldMember is not null)
                {
                    var attributesOnResolver = fieldMember.GetCustomAttributes().ToArray();

                    if (ContainsNamedAttribute(attributesOnResolver, AspNetCoreAuthorizeAttributeName))
                    {
                        completionContext.ReportError(
                            UnsupportedAspNetCoreAttributeError(
                                AspNetCoreAuthorizeAttributeName,
                                _authorizeAttributeName,
                                fieldMember));
                        return;
                    }

                    if (ContainsNamedAttribute(attributesOnResolver, AspNetCoreAllowAnonymousAttributeName))
                    {
                        completionContext.ReportError(
                            UnsupportedAspNetCoreAttributeError(
                                AspNetCoreAllowAnonymousAttributeName,
                                _allowAnonymousAttributeName,
                                fieldMember));
                        return;
                    }
                }
            }
        }
    }

    public override void OnAfterCompleteTypes()
    {
        foreach (var type in _objectTypes)
        {
            var objectType = (ObjectType)type.TypeReg.Type;

            if (objectType.ContextData.TryGetValue(NodeResolver, out var o) &&
                o is NodeResolverInfo nodeResolverInfo)
            {
                var pipeline = nodeResolverInfo.Pipeline;
                var directives = (DirectiveCollection)objectType.Directives;
                var length = directives.Count;
                ref var start = ref directives.GetReference();

                for (var i = length - 1; i >= 0; i--)
                {
                    var directive = Unsafe.Add(ref start, i);

                    if (directive.Type.Name.EqualsOrdinal(Authorize))
                    {
                        var authDir = directive.AsValue<AuthorizeDirective>();
                        pipeline = CreateAuthMiddleware(authDir).Middleware.Invoke(pipeline);
                    }
                }

                type.TypeDef.ContextData[NodeResolver] =
                    new NodeResolverInfo(nodeResolverInfo.QueryField, pipeline);
            }
        }
    }

    private void InspectObjectTypesForAuthDirective(State state)
    {
        foreach (var type in _objectTypes)
        {
            if (IsAuthorizedType(type.TypeDef))
            {
                var registration = type.TypeReg;
                var mainTypeRef = registration.TypeReference;

                // if this type is a root type we will copy type level auth down to the field.
                if (registration.IsQueryType == true ||
                    registration.IsMutationType == true ||
                    registration.IsSubscriptionType == true)
                {
                    foreach (var fieldDef in type.TypeDef.Fields)
                    {
                        // we are not interested in introspection fields or the node fields.
                        if (fieldDef.IsIntrospectionField || fieldDef.IsNodeField())
                        {
                            continue;
                        }

                        // if the field contains the AnonymousAllowed flag we will not
                        // apply authorization on it.
                        if (fieldDef.GetContextData().ContainsKey(AllowAnonymous))
                        {
                            continue;
                        }

                        ApplyAuthMiddleware(fieldDef, registration, false);
                    }
                }

                foreach (var reference in registration.References)
                {
                    state.AuthTypes.Add(reference);
                    state.NeedsAuth.Add(reference);
                }

                if (type.TypeDef.HasInterfaces)
                {
                    CollectInterfaces(
                        type.TypeDef.GetInterfaces(),
                        interfaceTypeRef =>
                        {
                            if (_typeRegistry.TryGetType(
                                interfaceTypeRef,
                                out var interfaceTypeReg))
                            {
                                foreach (var typeRef in interfaceTypeReg.References)
                                {
                                    state.NeedsAuth.Add(typeRef);

                                    if (!state.AbstractToConcrete.TryGetValue(
                                        typeRef,
                                        out var authTypeRefs))
                                    {
                                        authTypeRefs = [];
                                        state.AbstractToConcrete.Add(typeRef, authTypeRefs);
                                    }

                                    authTypeRefs.Add(mainTypeRef);
                                }
                            }
                        },
                        state);

                    state.Completed.Clear();
                }
            }
        }
    }

    private void FindUnionTypesThatContainAuthTypes(State state)
    {
        foreach (var type in _unionTypes)
        {
            var unionTypeReg = type.TypeReg;
            var unionTypeRef = unionTypeReg.TypeReference;
            List<TypeReference>? authTypeRefs = null;

            foreach (var memberTypeRef in type.TypeDef.Types)
            {
                if (state.AuthTypes.Contains(memberTypeRef))
                {
                    foreach (var typeRef in unionTypeReg.References)
                    {
                        state.NeedsAuth.Add(typeRef);
                    }

                    if (authTypeRefs is null &&
                        !state.AbstractToConcrete.TryGetValue(unionTypeRef, out authTypeRefs))
                    {
                        authTypeRefs = [];
                        state.AbstractToConcrete.Add(unionTypeRef, authTypeRefs);
                    }

                    authTypeRefs.Add(memberTypeRef);
                }
            }
        }
    }

    private void FindFieldsAndApplyAuthMiddleware(State state)
    {
        foreach (var type in _objectTypes)
        {
            if (state.AuthTypes.Contains(type.TypeRef))
            {
                CheckForValidationAuth(type);
            }

            var typeName = type.TypeDef.Name;

            foreach (var fieldDef in type.TypeDef.Fields)
            {
                ApplyAuthMiddleware(typeName, fieldDef, state);
            }
        }
    }

    private void HandleSpecialQueryFields(ObjectTypeInfo type, State state)
    {
        var options = state.Options;

        foreach (var fieldDef in type.TypeDef.Fields)
        {
            if (fieldDef.Name.EqualsOrdinal(IntrospectionFields.Type))
            {
                if (options.ConfigureTypeField is null)
                {
                    continue;
                }

                var descriptor = ObjectFieldDescriptor.From(_context, fieldDef);
                options.ConfigureTypeField?.Invoke(descriptor);
                descriptor.CreateDefinition();
            }
            else if (fieldDef.Name.EqualsOrdinal(IntrospectionFields.Schema))
            {
                if (options.ConfigureSchemaField is null)
                {
                    continue;
                }

                var descriptor = ObjectFieldDescriptor.From(_context, fieldDef);
                options.ConfigureSchemaField?.Invoke(descriptor);
                descriptor.CreateDefinition();
            }
            else if (fieldDef.IsNodeField())
            {
                if (options.ConfigureNodeFields is null)
                {
                    continue;
                }

                var descriptor = ObjectFieldDescriptor.From(_context, fieldDef);
                options.ConfigureNodeFields?.Invoke(descriptor);
                descriptor.CreateDefinition();
            }
        }
    }

    private void CheckForValidationAuth(ObjectTypeInfo type)
    {
        if (_schemaContextData.ContainsKey(AuthorizationRequestPolicy))
        {
            return;
        }

        var directives = GetOrCreateDirectives(type.TypeReg);
        var length = directives.Count;
        ref var start = ref directives.GetReference();

        for (var i = length - 1; i >= 0; i--)
        {
            var directive = Unsafe.Add(ref start, i);

            if (directive.Type.Name.EqualsOrdinal(Authorize))
            {
                var authDir = directive.AsValue<AuthorizeDirective>();

                if (authDir.Apply is ApplyPolicy.Validation)
                {
                    _schemaContextData[AuthorizationRequestPolicy] = true;
                    return;
                }
            }
        }
    }

    private void ApplyAuthMiddleware(
        string typeName,
        ObjectFieldDefinition fieldDef,
        State state)
    {
        // if the field contains the AnonymousAllowed flag we will not apply authorization
        // on it.
        if (fieldDef.GetContextData().ContainsKey(AllowAnonymous))
        {
            return;
        }

        var isNodeField = fieldDef.IsNodeField();

        if (fieldDef.Type is not null &&
            _typeLookup.TryNormalizeReference(fieldDef.Type, out var typeRef) &&
            state.NeedsAuth.Contains(typeRef))
        {
            var typeReg = GetTypeRegistration(typeRef);

            if (typeReg.Kind is TypeKind.Object)
            {
                ApplyAuthMiddleware(
                    fieldDef,
                    typeReg,
                    isNodeField);
            }
            else if (state.AbstractToConcrete.TryGetValue(
                typeReg.TypeReference,
                out var refs))
            {
                foreach (var objTypeRef in refs)
                {
                    typeReg = GetTypeRegistration(objTypeRef);
                    ApplyAuthMiddleware(
                        fieldDef,
                        typeReg,
                        isNodeField);
                }
            }
            else
            {
                throw ThrowHelper.UnauthorizedType(
                    new SchemaCoordinate(typeName, fieldDef.Name));
            }
        }
    }

    private void ApplyAuthMiddleware(
        ObjectFieldDefinition fieldDef,
        RegisteredType authTypeReg,
        bool isNodeField)
    {
        var insertPos = 0;

        // we try to locate the last auth middleware and insert after it.
        if (fieldDef.MiddlewareDefinitions.Count > 0)
        {
            for (var i = 0; i < fieldDef.MiddlewareDefinitions.Count; i++)
            {
                if (fieldDef.MiddlewareDefinitions[i].Key == WellKnownMiddleware.Authorization)
                {
                    insertPos = i + 1;
                }
                else
                {
                    break;
                }
            }
        }

        // next we get the collection of directives for the object type that needs to be secured.
        var directives = GetOrCreateDirectives(authTypeReg);
        var length = directives.Count;
        ref var start = ref directives.GetReference();

        for (var i = length - 1; i >= 0; i--)
        {
            var directive = Unsafe.Add(ref start, i);

            if (directive.Type.Name.EqualsOrdinal(Authorize))
            {
                var authDir = directive.AsValue<AuthorizeDirective>();

                // if the directive represents a validation policy that must be invoked during
                // validation we do not need a middleware and will skip applying one.
                if (authDir.Apply is ApplyPolicy.Validation)
                {
                    // but we must mark the schema to have auth validation policies.
                    _schemaContextData[AuthorizationRequestPolicy] = true;
                    continue;
                }

                // node fields are skipped as the authorization is already handled by the
                // node resolver pipelines.
                if (isNodeField)
                {
                    continue;
                }

                fieldDef.MiddlewareDefinitions.Insert(
                    insertPos++,
                    CreateAuthMiddleware(
                        authDir));
            }
        }
    }

    private static FieldMiddlewareDefinition CreateAuthMiddleware(
        AuthorizeDirective directive)
        => new FieldMiddlewareDefinition(
            next =>
            {
                // we capture the auth middleware instance on the outer factory delegate.
                // this avoids allocation of multiple new auth instances within the
                // pipeline.
                var auth = new AuthorizeMiddleware(
                    next,
                    directive);

                return async context => await auth.InvokeAsync(context);
            },
            isRepeatable: true,
            key: WellKnownMiddleware.Authorization);

    private DirectiveCollection GetOrCreateDirectives(RegisteredType registration)
    {
        var type = (ObjectType)registration.Type;

        if (_directives.TryGetValue(type, out var directives))
        {
            return (DirectiveCollection)directives;
        }

        var typeDef = type.Definition!;
        var directiveDefs = typeDef.GetDirectives();

        CompleteDirectiveTypes(directiveDefs);

        directives = DirectiveCollection.CreateAndComplete(
            registration,
            type,
            typeDef.GetDirectives());

        type.Directives = directives;
        _directives.Add(type, directives);

        return (DirectiveCollection)directives;
    }

    private void CompleteDirectiveTypes(IReadOnlyList<DirectiveDefinition> directives)
    {
        foreach (var directiveDef in directives)
        {
            CompleteDirectiveType(directiveDef);
        }
    }

    private void CompleteDirectiveType(DirectiveDefinition directive)
    {
        if (!_completedTypeRefs.Add(directive.Type))
        {
            return;
        }

        var current = GetTypeRegistration(directive.Type);

        if (_completedTypes.Add(current))
        {
            var discovery = new Stack<RegisteredType>();
            var completion = new Stack<RegisteredType>();
            discovery.Push(current);

            while (discovery.Count > 0)
            {
                current = discovery.Pop();
                completion.Push(current);

                foreach (var dependency in current.Dependencies)
                {
                    var next = GetTypeRegistration(dependency.Type);

                    if (_completedTypes.Add(next))
                    {
                        discovery.Push(next);
                    }
                }
            }

            while (completion.Count > 0)
            {
                _typeInitializer.CompleteType(completion.Pop());
            }
        }
    }

    private void CollectInterfaces(
        IReadOnlyList<TypeReference> interfaces,
        Action<TypeReference> register,
        State state)
    {
        state.Queue.AddRange(interfaces);

        while (state.Queue.Count > 0)
        {
            var current = state.Queue.Pop();

            if (state.Completed.Add(current))
            {
                var registration = GetTypeRegistration(current);
                register(registration.TypeReference);

                var typeDef = ((InterfaceType)registration.Type).Definition!;

                if (typeDef.HasInterfaces)
                {
                    state.Queue.AddRange(typeDef.Interfaces);
                }
            }
        }
    }

    private RegisteredType GetTypeRegistration(TypeReference typeReference)
    {
        if (_typeLookup.TryNormalizeReference(typeReference, out var normalizedTypeRef) &&
            _typeRegistry.TryGetType(normalizedTypeRef, out var registration))
        {
            return registration;
        }

        throw ThrowHelper.UnableToResolveTypeReg();
    }

    private static bool IsAuthorizedType<T>(T definition)
        where T : IHasDirectiveDefinition
    {
        if (!definition.HasDirectives)
        {
            return false;
        }

        var directives = (List<DirectiveDefinition>)definition.Directives;
        var length = directives.Count;

        ref var start = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(directives));

        for (var i = 0; i < length; i++)
        {
            var directiveDef = Unsafe.Add(ref start, i);

            if (directiveDef.Type is NameDirectiveReference { Name: Authorize, } ||
                (directiveDef.Type is ExtendedTypeDirectiveReference { Type.Type: { } type, } &&
                    type == typeof(AuthorizeDirective)))
            {
                return true;
            }
        }

        return false;
    }

    private State CreateState()
    {
        AuthorizationOptions? options = null;

        if (_context.ContextData.TryGetValue(
                WellKnownContextData.AuthorizationOptions,
                out var value) &&
            value is AuthorizationOptions opt)
        {
            options = opt;
        }

        return new State(options ?? new());
    }

    private static bool ContainsNamedAttribute(Attribute[] attributes, string nameOfAttribute)
        => attributes.Any(a => a.GetType().FullName == nameOfAttribute);

    private static ISchemaError UnsupportedAspNetCoreAttributeError(
        string aspNetCoreAttributeName,
        string properAttributeName,
        Type runtimeType)
    {
        return SchemaErrorBuilder.New()
            .SetMessage(string.Format(AuthorizationTypeInterceptor_UnsupportedAspNetCoreAttributeOnType,
                aspNetCoreAttributeName, runtimeType.FullName, properAttributeName))
            .SetCode(ErrorCodes.Schema.UnsupportedAspNetCoreAuthorizationAttribute)
            .Build();
    }

    private static ISchemaError UnsupportedAspNetCoreAttributeError(
        string aspNetCoreAttributeName,
        string properAttributeName,
        MemberInfo member)
    {
        var nameOfDeclaringType = member.DeclaringType?.FullName;
        var nameOfMember = member.Name;

        return SchemaErrorBuilder.New()
            .SetMessage(string.Format(AuthorizationTypeInterceptor_UnsupportedAspNetCoreAttributeOnMember,
                aspNetCoreAttributeName, nameOfDeclaringType, nameOfMember, properAttributeName))
            .SetCode(ErrorCodes.Schema.UnsupportedAspNetCoreAuthorizationAttribute)
            .Build();
    }
}

static file class AuthorizationTypeInterceptorExtensions
{
    public static bool IsNodeField(this ObjectFieldDefinition fieldDef)
    {
        var contextData = fieldDef.GetContextData();

        return contextData.ContainsKey(WellKnownContextData.IsNodeField) ||
            contextData.ContainsKey(WellKnownContextData.IsNodesField);
    }
}
