using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using static HotChocolate.Authorization.AuthorizeDirectiveType.Names;

namespace HotChocolate.Authorization;

internal sealed partial class AuthorizationTypeInterceptor : TypeInterceptor
{
    private readonly List<ObjectTypeInfo> _objectTypes = [];
    private readonly List<UnionTypeInfo> _unionTypes = [];
    private readonly Dictionary<ObjectType, DirectiveCollection> _directives = [];
    private readonly HashSet<TypeReference> _completedTypeRefs = [];
    private readonly HashSet<RegisteredType> _completedTypes = [];
    private State? _state;
    private IDescriptorContext _context = null!;
    private TypeInitializer _typeInitializer = null!;
    private TypeRegistry _typeRegistry = null!;
    private TypeLookup _typeLookup = null!;
    private SchemaTypeConfiguration _schemaConfig = null!;
    private ITypeCompletionContext _queryContext = null!;
    private ITypeCompletionContext _authDirectiveContext = null!;
    private AuthorizeDirectiveType _authDirective = null!;

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
        schemaBuilder.SetSchema(d => _schemaConfig = d.Extend().Configuration);
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        switch (completionContext.Type)
        {
            // at this point we collect object types so we can check if they need to be authorized.
            case ObjectType when configuration is ObjectTypeConfiguration objectTypeDef:
                _objectTypes.Add(new ObjectTypeInfo(completionContext, objectTypeDef));
                break;

            // also we collect union types so we can see if a union exposes
            // an authorized object type.
            case UnionType when configuration is UnionTypeConfiguration unionTypeDef:
                _unionTypes.Add(new UnionTypeInfo(completionContext, unionTypeDef));
                break;

            case AuthorizeDirectiveType type:
                _authDirective = type;
                _authDirectiveContext = completionContext;
                break;
        }

        // note, we do not need to collect interfaces as the object type has a
        // list implement that links to the interfaces that expose an object type.
    }

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryContext = completionContext;
        }
    }

    public override void OnBeforeCompleteMetadata()
    {
        _authDirective.CompleteMetadata(_authDirectiveContext);
        ((RegisteredType)_authDirectiveContext).Status = TypeStatus.MetadataCompleted;

        // at this stage in the type initialization we will create some state that we
        // will use to transform the schema authorization.
        var state = _state = CreateState();

        // copy temporary state to schema state.
        if (_context.IsAuthorizedAtRequestLevel())
        {
            _schemaConfig.ModifyAuthorizationFieldOptions(o => o with { AuthorizeAtRequestLevel = true });
        }

        // before we can apply schema transformations, we will inspect the object types
        // to identify the ones that are protected with authorization directives.
        InspectObjectTypesForAuthDirective(state);

        // next we will inspect the union types that expose one or more protected object types.
        FindUnionTypesThatContainAuthTypes(state);

        // at last, we will find fields that expose protected types and apply authorization
        // middleware.
        FindFieldsAndApplyAuthMiddleware(state);
    }

    public override void OnBeforeCompleteMetadata(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        // last in the initialization we need to intercept the query type and ensure that
        // authorization configuration is applied to the special introspection and node fields.
        if (ReferenceEquals(_queryContext, completionContext) &&
            configuration is ObjectTypeConfiguration typeDef)
        {
            var state = _state ?? throw ThrowHelper.StateNotInitialized();
            HandleSpecialQueryFields(new ObjectTypeInfo(completionContext, typeDef), state);
        }
    }

    public override void OnAfterMakeExecutable()
    {
        foreach (var type in _objectTypes)
        {
            var objectType = (ObjectType)type.TypeReg.Type;

            if (!objectType.TryGetNodeResolver(out var nodeResolverInfo))
            {
                continue;
            }

            var pipeline = nodeResolverInfo.Pipeline;
            var directives = objectType.Directives;
            var length = directives.Count;
            ref var start = ref directives.GetReference();

            for (var i = length - 1; i >= 0; i--)
            {
                var directive = Unsafe.Add(ref start, i);

                if (directive.Type.Name.EqualsOrdinal(Authorize))
                {
                    var authDir = directive.ToValue<AuthorizeDirective>();
                    pipeline = CreateAuthMiddleware(authDir).Middleware.Invoke(pipeline);
                }
            }

            objectType.SetNodeResolver(new NodeResolverInfo(nodeResolverInfo.QueryField, pipeline));
        }
    }

    private void InspectObjectTypesForAuthDirective(State state)
    {
        foreach (var type in _objectTypes)
        {
            if (!IsAuthorizedType(type.TypeDef))
            {
                continue;
            }

            var registration = type.TypeReg;
            var mainTypeRef = registration.TypeReference;

            // if this type is a root type, we will copy type level auth down to the field.
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

                    // if the field contains the AnonymousAllowed flag, we will not
                    // apply authorization on it.
                    if (fieldDef.IsAnonymousAllowed())
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

            if (!type.TypeDef.HasInterfaces)
            {
                continue;
            }

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
            if (fieldDef.Name.EqualsOrdinal(IntrospectionFieldNames.Type))
            {
                if (options.ConfigureTypeField is null)
                {
                    continue;
                }

                var descriptor = ObjectFieldDescriptor.From(_context, fieldDef);
                options.ConfigureTypeField?.Invoke(descriptor);
                descriptor.CreateConfiguration();
            }
            else if (fieldDef.Name.EqualsOrdinal(IntrospectionFieldNames.Schema))
            {
                if (options.ConfigureSchemaField is null)
                {
                    continue;
                }

                var descriptor = ObjectFieldDescriptor.From(_context, fieldDef);
                options.ConfigureSchemaField?.Invoke(descriptor);
                descriptor.CreateConfiguration();
            }
            else if (fieldDef.IsNodeField())
            {
                if (options.ConfigureNodeFields is null)
                {
                    continue;
                }

                var descriptor = ObjectFieldDescriptor.From(_context, fieldDef);
                options.ConfigureNodeFields?.Invoke(descriptor);
                descriptor.CreateConfiguration();
            }
        }
    }

    private void CheckForValidationAuth(ObjectTypeInfo type)
    {
        if (_schemaConfig.IsAuthorizedAtRequestLevel())
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
                var authDir = directive.ToValue<AuthorizeDirective>();

                if (authDir.Apply is ApplyPolicy.Validation)
                {
                    _schemaConfig.ModifyAuthorizationFieldOptions(o => o with { AuthorizeAtRequestLevel = true });
                    return;
                }
            }
        }
    }

    private void ApplyAuthMiddleware(
        string typeName,
        ObjectFieldConfiguration fieldDef,
        State state)
    {
        // if the field contains the AnonymousAllowed flag, we will not apply authorization
        // on it.
        if (fieldDef.IsAnonymousAllowed())
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
        ObjectFieldConfiguration fieldDef,
        RegisteredType authTypeReg,
        bool isNodeField)
    {
        var insertPos = 0;

        // we try to locate the last auth middleware and insert after it.
        if (fieldDef.MiddlewareConfigurations.Count > 0)
        {
            for (var i = 0; i < fieldDef.MiddlewareConfigurations.Count; i++)
            {
                if (fieldDef.MiddlewareConfigurations[i].Key == WellKnownMiddleware.Authorization)
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
                var authDir = directive.ToValue<AuthorizeDirective>();

                // if the directive represents a validation policy that must be invoked during
                // validation, we do not need middleware and will skip applying one.
                if (authDir.Apply is ApplyPolicy.Validation)
                {
                    // but we must mark the schema to have auth validation policies.
                    _schemaConfig.ModifyAuthorizationFieldOptions(o => o with { AuthorizeAtRequestLevel = true });
                    continue;
                }

                // node fields are skipped as the authorization is already handled by the
                // node resolver pipelines.
                if (isNodeField)
                {
                    continue;
                }

                fieldDef.MiddlewareConfigurations.Insert(
                    insertPos++,
                    CreateAuthMiddleware(
                        authDir));
            }
        }
    }

    private static FieldMiddlewareConfiguration CreateAuthMiddleware(
        AuthorizeDirective directive)
        => new FieldMiddlewareConfiguration(
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
            return directives;
        }

        var typeDef = type.Configuration!;
        var directiveDefs = typeDef.GetDirectives();

        CompleteDirectiveTypes(directiveDefs);

        directives = DirectiveCollection.CreateAndComplete(
            registration,
            type,
            typeDef.GetDirectives());

        type.Directives = directives;
        _directives.Add(type, directives);

        return directives;
    }

    private void CompleteDirectiveTypes(IReadOnlyList<DirectiveConfiguration> directives)
    {
        foreach (var directiveDef in directives)
        {
            CompleteDirectiveType(directiveDef);
        }
    }

    private void CompleteDirectiveType(DirectiveConfiguration directive)
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

                var typeDef = ((InterfaceType)registration.Type).Configuration!;

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
        where T : IDirectiveConfigurationProvider
    {
        if (!definition.HasDirectives)
        {
            return false;
        }

        var directives = (List<DirectiveConfiguration>)definition.Directives;
        var length = directives.Count;

        ref var start = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(directives));

        for (var i = 0; i < length; i++)
        {
            var directiveDef = Unsafe.Add(ref start, i);

            if (directiveDef.Type is NameDirectiveReference { Name: Authorize }
                || (directiveDef.Type is ExtendedTypeDirectiveReference { Type.Type: { } type }
                    && type == typeof(AuthorizeDirective)))
            {
                return true;
            }
        }

        return false;
    }

    private State CreateState()
        => new(_context.GetAuthorizationOptions());
}

file static class AuthorizationTypeInterceptorExtensions
{
    public static bool IsNodeField(this ObjectFieldConfiguration fieldDef)
    {
        return (fieldDef.Flags & CoreFieldFlags.GlobalIdNodeField) == CoreFieldFlags.GlobalIdNodeField
            || (fieldDef.Flags & CoreFieldFlags.GlobalIdNodesField) == CoreFieldFlags.GlobalIdNodesField;
    }
}
