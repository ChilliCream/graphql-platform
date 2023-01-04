using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
    private readonly HashSet<ITypeReference> _completedTypeRefs = new();
    private readonly HashSet<RegisteredType> _completedTypes = new();
    private readonly HashSet<ITypeCompletionContext> _rootTypes = new();

    private IDescriptorContext _context = default!;
    private TypeInitializer _typeInitializer = default!;
    private TypeRegistry _typeRegistry = default!;
    private TypeLookup _typeLookup = default!;

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

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition,
        OperationType operationType)
        => _rootTypes.Add(completionContext);

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (completionContext.Type is ObjectType &&
            definition is ObjectTypeDefinition objectTypeDef)
        {
            _objectTypes.Add(new ObjectTypeInfo(completionContext, objectTypeDef));
        }
        else if (completionContext.Type is UnionType &&
            definition is UnionTypeDefinition unionTypeDef)
        {
            _unionTypes.Add(new UnionTypeInfo(completionContext, unionTypeDef));
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        var state = CreateState();
        InspectObjectTypesForAuthDirective(state);
        FindUnionTypesThatContainAuthTypes(state);
        FindFieldsAndApplyAuthMiddleware(state);
    }

    private void InspectObjectTypesForAuthDirective(State state)
    {
        foreach (var type in _objectTypes)
        {
            if (IsAuthorizedType(type.TypeDef))
            {
                var registration = (RegisteredType)type.Context;
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
                                authTypeRefs = new List<ITypeReference>();
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
            var unionTypeReg = (RegisteredType)type.Context;
            var unionTypeRef = unionTypeReg.TypeReference;
            List<ITypeReference>? authTypeRefs = null;

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
                        authTypeRefs = new List<ITypeReference>();
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
            // if the current type is a root type and the root type itself is protected,
            // we will protect each of its fields.
            if (_rootTypes.Contains(type.Context) && state.NeedsAuth.Contains(type.TypeRef))
            {
                var isQueryType = type.Context.IsQueryType ?? false;
                var typeReg = (RegisteredType)type.Context;
                var options = state.Options;

                foreach (var fieldDef in type.TypeDef.Fields)
                {
                    var contextData = fieldDef.GetContextData();

                    if (!isQueryType)
                    {
                        ApplyAuthMiddleware(fieldDef, typeReg, schemaServices);
                    }
                    else if (fieldDef.Name.EqualsOrdinal(IntrospectionFields.TypeName))
                    {
                        if (!options.SkipTypeNameField)
                        {
                            ApplyAuthMiddleware(fieldDef, typeReg, schemaServices);
                        }

                        if (options.ConfigureTypeNameField is not null)
                        {
                            // ObjectFieldDescriptor.From(_context, fieldDef)
                            // options.ConfigureTypeNameField?.Invoke();
                        }
                    }
                    else if (fieldDef.Name.EqualsOrdinal(IntrospectionFields.Type))
                    {

                    }
                    else if (fieldDef.Name.EqualsOrdinal(IntrospectionFields.Schema))
                    {

                    }
                    else if (contextData.ContainsKey(IsNodeField) ||
                        contextData.ContainsKey(IsNodeField))
                    {

                    }
                    else
                    {

                    }
                }
            }

            foreach (var fieldDef in type.TypeDef.Fields)
            {
                if (fieldDef.Type is not null &&
                    _typeLookup.TryNormalizeReference(fieldDef.Type, out var typeRef) &&
                    state.NeedsAuth.Contains(typeRef))
                {
                    var typeReg = GetTypeRegistration(typeRef);

                    if (typeReg.Kind is TypeKind.Object)
                    {
                        ApplyAuthMiddleware(fieldDef, typeReg, schemaServices);
                    }
                    else if (state.AbstractToConcrete.TryGetValue(
                        typeReg.TypeReference,
                        out var refs))
                    {
                        foreach (var objTypeRef in refs)
                        {
                            typeReg = GetTypeRegistration(objTypeRef);
                            ApplyAuthMiddleware(fieldDef, typeReg, schemaServices);
                        }
                    }
                    else
                    {
                        // TODO : Errors
                        throw new InvalidOperationException("should not happen!");
                    }
                }
            }
        }
    }

    private void ApplyAuthMiddleware(
        ObjectFieldDefinition fieldDef,
        RegisteredType authTypeReg,
        IServiceProvider schemaServices)
    {
        var directives = GetOrCreateDirectives(authTypeReg);
        var length = directives.Count;
        var start = directives.GetReference();

        for (var i = length - 1; i >= 0; i--)
        {
            fieldDef.MiddlewareDefinitions.Insert(
                0,
                CreateAuthMiddleware(
                    Unsafe.Add(ref start, i),
                    schemaServices));
        }
    }

    private static FieldMiddlewareDefinition CreateAuthMiddleware(
        Directive directive,
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
                    directive.AsValue<AuthorizeDirective>());

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
        IReadOnlyList<ITypeReference> interfaces,
        Action<ITypeReference> register,
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

    private RegisteredType GetTypeRegistration(ITypeReference typeReference)
    {
        if (_typeLookup.TryNormalizeReference(typeReference, out var normalizedTypeRef) &&
            _typeRegistry.TryGetType(normalizedTypeRef, out var registration))
        {
            return registration;
        }

        throw new InvalidOperationException("This should not happen at this point!");
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
        var start = MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(directives));
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

    private sealed class State
    {
        public State(AuthorizationOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Provides access to the authorization options.
        /// </summary>
        public AuthorizationOptions Options { get; }

        /// <summary>
        ///  Gets the types to which authorization middleware need to be applied.
        /// </summary>
        public HashSet<ITypeReference> NeedsAuth { get; } = new();

        /// <summary>
        /// Gets the types to which are annotated with the @authorize directive.
        /// </summary>
        public HashSet<ITypeReference> AuthTypes { get; } = new();

        /// <summary>
        /// Gets a lookup table from abstract types to concrete types that need authorization.
        /// </summary>
        public Dictionary<ITypeReference, List<ITypeReference>> AbstractToConcrete { get; } = new();

        /// <summary>
        /// Gets a helper queue for processing types.
        /// </summary>
        public List<ITypeReference> Queue { get; } = new();

        /// <summary>
        /// Gets a helper set for tracking process completion.
        /// </summary>
        public HashSet<ITypeReference> Completed { get; } = new();
    }

    private sealed class ObjectTypeInfo : TypeInfo<ObjectTypeDefinition>
    {
        public ObjectTypeInfo(ITypeCompletionContext context, ObjectTypeDefinition typeDef)
            : base(context, typeDef)
        {
            TypeRef = ((RegisteredType)context).TypeReference;
        }

        public ITypeReference TypeRef { get; }
    }

    private sealed class UnionTypeInfo : TypeInfo<UnionTypeDefinition>
    {
        public UnionTypeInfo(ITypeCompletionContext context, UnionTypeDefinition typeDef)
            : base(context, typeDef) { }
    }

    private abstract class TypeInfo<TDef> : IEquatable<TypeInfo<TDef>> where TDef : DefinitionBase
    {
        protected TypeInfo(ITypeCompletionContext context, TDef typeDef)
        {
            Context = context;
            TypeDef = typeDef;
        }

        public ITypeCompletionContext Context { get; }

        public TDef TypeDef { get; }

        public bool Equals(TypeInfo<TDef>? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return TypeDef.Equals(other.TypeDef);
        }

        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj) ||
                (obj is ObjectTypeInfo other && Equals(other));

        public override int GetHashCode()
            => TypeDef.GetHashCode();
    }
}
