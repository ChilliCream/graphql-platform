using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Authorization.AuthorizeDirectiveType.Names;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Authorization;

internal sealed partial class AuthorizationTypeInterceptor : TypeInterceptor
{
    private readonly List<ObjectTypeInfo> _objectTypes = new();
    private readonly List<UnionTypeInfo> _unionTypes = new();
    private readonly Dictionary<ObjectType, IDirectiveCollection> _directives = new();
    private readonly HashSet<TypeReference> _completedTypeRefs = new();
    private readonly HashSet<RegisteredType> _completedTypes = new();
    private State? _state;

    private IDescriptorContext _context = default!;
    private TypeInitializer _typeInitializer = default!;
    private TypeRegistry _typeRegistry = default!;
    private TypeLookup _typeLookup = default!;
    private ExtensionData _schemaContextData = default!;

    internal override uint Position => uint.MaxValue;

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

    public override void OnBeforeCreateSchema(
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

        // before we can apply schema transformations we will inspect the object types
        // to identify the ones that are protected with authorization directives.
        InspectObjectTypesForAuthDirective(state);

        // next we will inspect the union types that expose one or more protected object types.
        FindUnionTypesThatContainAuthTypes(state);

        // last we will find fields that expose protected types and apply authorization
        // middleware.
        FindFieldsAndApplyAuthMiddleware(state);
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        // last in the initialization we need to intercept the query type and ensure that
        // authorization configuration is applied to the special introspection and node fields.
        if ((completionContext.IsQueryType ?? false) &&
            definition is ObjectTypeDefinition typeDef)
        {
            var state = _state ?? throw ThrowHelper.StateNotInitialized();
            HandleSpecialQueryFields(new ObjectTypeInfo(completionContext, typeDef), state);
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
                            state.NeedsAuth.Add(interfaceTypeRef);

                            if (!state.AbstractToConcrete.TryGetValue(
                                interfaceTypeRef,
                                out var authTypeRefs))
                            {
                                authTypeRefs = new List<TypeReference>();
                                state.AbstractToConcrete.Add(interfaceTypeRef, authTypeRefs);
                            }

                            authTypeRefs.Add(mainTypeRef);
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
                        authTypeRefs = new List<TypeReference>();
                        state.AbstractToConcrete.Add(unionTypeRef, authTypeRefs);
                    }

                    authTypeRefs.Add(memberTypeRef);
                }
            }
        }
    }

    private void FindFieldsAndApplyAuthMiddleware(State state)
    {
        var schemaServices = _context.Services;

        foreach (var type in _objectTypes)
        {
            if (state.AuthTypes.Contains(type.TypeRef))
            {
                CheckForValidationAuth(type);
            }

            var typeName = type.TypeDef.Name;

            foreach (var fieldDef in type.TypeDef.Fields)
            {
                ApplyAuthMiddleware(typeName, fieldDef, schemaServices, state);
            }
        }
    }

    private void HandleSpecialQueryFields(ObjectTypeInfo type, State state)
    {
        var options = state.Options;

        foreach (var fieldDef in type.TypeDef.Fields)
        {
            var contextData = fieldDef.GetContextData();

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
            else if (contextData.ContainsKey(IsNodeField) ||
                contextData.ContainsKey(IsNodeField))
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
        IServiceProvider schemaServices,
        State state)
    {
        var isNodeField = fieldDef.ContextData.ContainsKey(IsNodeField) ||
            fieldDef.ContextData.ContainsKey(IsNodeField);

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
                    schemaServices,
                    isNodeField,
                    state.Options);
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
                        schemaServices,
                        isNodeField,
                        state.Options);
                }
            }
            else
            {
                throw ThrowHelper.UnauthorizedType(
                    new FieldCoordinate(typeName, fieldDef.Name));
            }
        }
    }

    private void ApplyAuthMiddleware(
        ObjectFieldDefinition fieldDef,
        RegisteredType authTypeReg,
        IServiceProvider schemaServices,
        bool isNodeField,
        AuthorizationOptions options)
    {
        var directives = GetOrCreateDirectives(authTypeReg);
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
                    continue;
                }

                if (isNodeField && (options?.SkipNodeFields(authDir) ?? false))
                {
                    continue;
                }

                fieldDef.MiddlewareDefinitions.Insert(
                    0,
                    CreateAuthMiddleware(
                        authDir,
                        schemaServices));
            }
        }
    }

    private static FieldMiddlewareDefinition CreateAuthMiddleware(
        AuthorizeDirective directive,
        IServiceProvider schemaServices)
        => new FieldMiddlewareDefinition(
            next =>
            {
                // we capture the auth middleware instance on the outer factory delegate.
                // this avoids allocation of multiple new auth instances within the
                // pipeline.
                var auth = new AuthorizeMiddleware(
                    next,
                    schemaServices.GetApplicationService<IAuthorizationHandler>(),
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

#if NET6_0_OR_GREATER
        ref var start = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(directives));
#endif

        for (var i = 0; i < length; i++)
        {
#if NET6_0_OR_GREATER
            var directiveDef = Unsafe.Add(ref start, i);
#else
            var directiveDef = directives[i];
#endif

            if (directiveDef.Type is NameDirectiveReference { Name: Authorize } ||
                (directiveDef.Type is ExtendedTypeDirectiveReference { Type.Type: { } type } &&
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
}
