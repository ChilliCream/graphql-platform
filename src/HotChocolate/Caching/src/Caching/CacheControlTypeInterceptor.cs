using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Caching;

internal sealed class CacheControlTypeInterceptor : TypeInterceptor
{
    private bool _cacheControlOptionsResolved;
    private ICacheControlOptions _cacheControlOptions = default!;
    private TypeRegistry _typeRegistry = default!;
    private TypeLookup _typeLookup = default!;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _typeRegistry = typeRegistry;
        _typeLookup = typeLookup;
    }

    public override void OnBeforeRegisterDependencies(
       ITypeDiscoveryContext discoveryContext,
       DefinitionBase? definition,
       IDictionary<string, object?> contextData)
    {
        EnsureCacheControlSettingsAreLoaded(discoveryContext.DescriptorContext);

        if (!_cacheControlOptions.Enable)
        {
            return;
        }

        if (_cacheControlOptions.ApplyDefaults &&
            !discoveryContext.IsIntrospectionType &&
            definition is ObjectTypeDefinition objectDef &&
            objectDef.Fields.Any(CanApplyDefaultCacheControl))
        {
            IExtendedType directive =
                discoveryContext.TypeInspector.GetType(typeof(CacheControlDirectiveType));

            discoveryContext.Dependencies.Add(new(
                TypeReference.Create(directive),
                TypeDependencyKind.Completed));
        }
    }

    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext,
        DefinitionBase? definition, IDictionary<string, object?> contextData)
    {
        if (!_cacheControlOptions.Enable || !_cacheControlOptions.ApplyDefaults)
        {
            return;
        }

        if (completionContext.IsIntrospectionType ||
            completionContext.IsSubscriptionType == true ||
            completionContext.IsMutationType == true ||
            definition is not ObjectTypeDefinition objectDef)
        {
            return;
        }

        // todo: what about interface fields?
        foreach (ObjectFieldDefinition field in objectDef.Fields)
        {
            if (!CanApplyDefaultCacheControl(field))
            {
                continue;
            }

            ITypeReference? typeRef = field.Type;

            if (!_typeLookup.TryNormalizeReference(typeRef!, out typeRef) ||
                !_typeRegistry.TryGetType(typeRef, out RegisteredType? type))
            {
                // todo: maybe throw error
                continue;
            }

            var isComplexType = type.Kind is TypeKind.Object or TypeKind.Interface or TypeKind.Union;

            if (completionContext.IsQueryType == true ||
                isComplexType ||
                IsDataResolver(field))
            {
                ApplyCacheControlWithDefaultMaxAge(field);
            }
            else if (type.Kind == TypeKind.Scalar)
            {
                ApplyCacheControlWithInheritMaxAge(field);
            }
        }
    }

    private void ApplyCacheControlWithDefaultMaxAge(ObjectFieldDefinition field)
    {
        field.Directives.Add(
            new DirectiveDefinition(
                new DirectiveNode(
                    "cacheControl",
                    new ArgumentNode("maxAge", _cacheControlOptions.DefaultMaxAge))));
    }

    private static void ApplyCacheControlWithInheritMaxAge(ObjectFieldDefinition field)
    {
        field.Directives.Add(
            new DirectiveDefinition(
                new DirectiveNode(
                    "cacheControl",
                    new ArgumentNode("inheritMaxAge", new BooleanValueNode(true)))));
    }

    // todo: make reusable, exists also in CostTypeInterceptor
    private static bool IsDataResolver(ObjectFieldDefinition field)
    {
        if (field.PureResolver is not null && field.MiddlewareDefinitions.Count == 0)
        {
            return false;
        }

        if (field.Resolver is not null)
        {
            return true;
        }

        MemberInfo? resolver = field.ResolverMember ?? field.Member;

        if (resolver is MethodInfo method)
        {
            if (typeof(Task).IsAssignableFrom(method.ReturnType) ||
                typeof(IQueryable).IsAssignableFrom(method.ReturnType) ||
                typeof(IExecutable).IsAssignableFrom(method.ReturnType))
            {
                return true;
            }

            if (method.ReturnType.IsGenericType &&
                method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                return true;
            }
        }

        return false;
    }

    // todo: check if field type contains annotation
    private static bool CanApplyDefaultCacheControl(ObjectFieldDefinition field)
    {
        if (field.IsIntrospectionField)
        {
            return false;
        }

        IReadOnlyList<DirectiveDefinition> directives = field.GetDirectives();
        return directives.Count == 0 || !directives.Any(IsCacheControlDirective);
    }

    private static bool IsCacheControlDirective(DirectiveDefinition directive)
    {
        if (directive.Reference is NameDirectiveReference { Name: { Value: "cacheControl" } })
        {
            return true;
        }

        if (directive.Reference is ClrTypeDirectiveReference { ClrType: { } type } &&
            type == typeof(CacheControlDirective))
        {
            return true;
        }

        return false;
    }

    private void EnsureCacheControlSettingsAreLoaded(IDescriptorContext descriptorContext)
    {
        if (!_cacheControlOptionsResolved)
        {
            // todo: load proper settings
            _cacheControlOptions =
                descriptorContext.ContextData.TryGetValue("TODO", out var value) &&
                value is ICacheControlOptions cacheControlOptions
                    ? cacheControlOptions
                    : new CacheControlOptions();

            _cacheControlOptionsResolved = true;
        }
    }
}