using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Authorization.AuthorizeDirectiveType.Names;

namespace HotChocolate.Authorization;

internal sealed class AuthorizationTypeInterceptor : TypeInterceptor
{
    private readonly List<CompositeTypeInfo> _compositeTypes = new();
    private readonly List<ObjectTypeInfo> _objectTypes = new();
    private readonly Dictionary<ObjectType, IDirectiveCollection> _directives = new();
    private readonly HashSet<ITypeReference> _completedTypeRefs = new();
    private readonly HashSet<RegisteredType> _completedTypes = new();

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
        _typeInitializer = typeInitializer;
        _typeRegistry = typeRegistry;
        _typeLookup = typeLookup;
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (completionContext.Type is ObjectType &&
            definition is ObjectTypeDefinition objectTypeDef)
        {
            _compositeTypes.Add(
                new CompositeTypeInfo(
                    completionContext,
                    objectTypeDef));

            _objectTypes.Add(
                new ObjectTypeInfo(
                    completionContext,
                    objectTypeDef));
        }
        else if (completionContext.Type is UnionType &&
            definition is UnionTypeDefinition unionTypeDef)
        {
            _compositeTypes.Add(
                new CompositeTypeInfo(
                    completionContext,
                    unionTypeDef));
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        var needsAuth = new HashSet<ITypeReference>();
        var authTypes = new HashSet<ITypeReference>();
        var abstractLookup = new Dictionary<ITypeReference, List<ITypeReference>>();
        var typeRefQueue = new List<ITypeReference>();
        var completed = new HashSet<ITypeReference>();

        foreach (var typeInfo in _objectTypes)
        {
            if (IsAuthorizedType(typeInfo.TypeDef))
            {
                var registration = (RegisteredType)typeInfo.Context;
                var mainTypeRef = registration.TypeReference;

                foreach (var reference in registration.References)
                {
                    authTypes.Add(reference);
                    needsAuth.Add(reference);
                }

                if (typeInfo.TypeDef.HasInterfaces)
                {
                    CollectInterfaces(
                        typeRefQueue,
                        interfaceTypeRef =>
                        {
                            needsAuth.Add(interfaceTypeRef);

                            if (!abstractLookup.TryGetValue(interfaceTypeRef, out var authTypeRefs))
                            {
                                authTypeRefs = new List<ITypeReference>();
                                abstractLookup.Add(interfaceTypeRef, authTypeRefs);
                            }

                            authTypeRefs.Add(mainTypeRef);
                        },
                        completed);

                    completed.Clear();
                }
            }
        }

        foreach (var refType in _compositeTypes)
        {
            if (refType.TypeDef is UnionTypeDefinition unionTypeDef)
            {
                var unionTypeReg = (RegisteredType)refType.Context;
                var mainUnionTypeRef = unionTypeReg.TypeReference;
                List<ITypeReference>? authTypeRefs = null;

                foreach (var memberTypeRef in unionTypeDef.Types)
                {
                    if (authTypes.Contains(memberTypeRef))
                    {
                        foreach (var unionTypeRef in unionTypeReg.References)
                        {
                            needsAuth.Add(unionTypeRef);
                        }

                        if (authTypeRefs is null &&
                            !abstractLookup.TryGetValue(mainUnionTypeRef, out authTypeRefs))
                        {
                            authTypeRefs = new List<ITypeReference>();
                            abstractLookup.Add(mainUnionTypeRef, authTypeRefs);
                        }

                        authTypeRefs.Add(memberTypeRef);
                    }
                }
            }
        }

        var schemaServices = _objectTypes.First().Context.DescriptorContext.Services;

        foreach (var refType in _compositeTypes)
        {
            if (refType.TypeDef is ObjectTypeDefinition objectTypeDef)
            {
                foreach (var fieldDef in objectTypeDef.Fields)
                {
                    if (fieldDef.Type is not null &&
                        _typeLookup.TryNormalizeReference(fieldDef.Type, out var typeRef) &&
                        needsAuth.Contains(typeRef))
                    {
                        var typeReg = GetTypeRegistration(typeRef);

                        if (typeReg.Kind is TypeKind.Object)
                        {
                            ApplyAuthMiddleware(fieldDef, typeReg, schemaServices);
                        }
                        else if (abstractLookup.TryGetValue(typeReg.TypeReference, out var refs))
                        {
                            foreach (var objTypeRef in refs)
                            {
                                typeReg = GetTypeRegistration(objTypeRef);
                                ApplyAuthMiddleware(fieldDef, typeReg, schemaServices);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("should not happen!");
                        }
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

    private FieldMiddlewareDefinition CreateAuthMiddleware(
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
            key: "Auth"); // TODO : well known

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
        List<ITypeReference> interfaces,
        Action<ITypeReference> register,
        HashSet<ITypeReference> completed)
    {
        while (interfaces.Count > 0)
        {
            var current = interfaces.Pop();

            if (completed.Add(current))
            {
                var registration = GetTypeRegistration(current);
                register(registration.TypeReference);

                var typeDef = ((InterfaceType)registration.Type).Definition!;

                if (typeDef.HasInterfaces)
                {
                    interfaces.AddRange(typeDef.Interfaces);
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

    private static bool IsAuthorizedType(ObjectTypeDefinition typeDef)
    {
        if (!typeDef.HasDirectives)
        {
            return false;
        }

        var directives = (List<DirectiveDefinition>)typeDef.Directives;
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
                directiveDef.Type is ExtendedTypeDirectiveReference { Type.Type: { } type } &&
                type == typeof(AuthorizeDirective))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class ObjectTypeInfo : IEquatable<ObjectTypeInfo>
    {
        public ObjectTypeInfo(ITypeCompletionContext context, ObjectTypeDefinition typeDef)
        {
            Context = context;
            TypeDef = typeDef;
        }

        public ITypeCompletionContext Context { get; }

        public ObjectTypeDefinition TypeDef { get; }

        public bool Equals(ObjectTypeInfo? other)
        {
            if (ReferenceEquals(null, other))
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
                obj is ObjectTypeInfo other && Equals(other);

        public override int GetHashCode()
            => TypeDef.GetHashCode();
    }

    private sealed class CompositeTypeInfo
    {
        public CompositeTypeInfo(ITypeCompletionContext context, DefinitionBase typeDef)
        {
            Context = context;
            TypeDef = typeDef;
        }

        public ITypeCompletionContext Context { get; }

        public DefinitionBase TypeDef { get; }

        public bool Equals(CompositeTypeInfo? other)
        {
            if (ReferenceEquals(null, other))
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
                obj is CompositeTypeInfo other && Equals(other);

        public override int GetHashCode()
            => TypeDef.GetHashCode();
    }
}
