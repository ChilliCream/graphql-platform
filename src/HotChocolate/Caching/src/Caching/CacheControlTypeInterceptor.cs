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
            completionContext.IsMutationType == true)
        {
            return;
        }

        if (definition is ObjectTypeDefinition objectDef)
        {
            foreach (ObjectFieldDefinition field in objectDef.Fields)
            {
                if (field.IsIntrospectionField)
                {
                    // Introspection fields do not need to be declared as cachable.
                    continue;
                }

                // todo: if this does not specify anything regarding maxAge, we should still consider it
                if (!CanApplyCacheControlDirective(field))
                {
                    continue;
                }

                if (completionContext.IsQueryType == true || IsDataResolver(field))
                {
                    // Each field on the query type or data resolver fields
                    // are treated as fields that need to be explicitly cached.
                    ApplyCacheControlWithDefaultMaxAge(field);

                    continue;
                }

                MarkField(field);

                // todo: handle @cacheControl existing on interface field
            }
        }
        else if (definition is InterfaceTypeDefinition interfaceDef)
        {
            foreach (InterfaceFieldDefinition field in interfaceDef.Fields)
            {
                MarkField(field);
            }
        }
    }

    // todo: rename, handle directive with maxage on type, etc.
    private void MarkField(OutputFieldDefinitionBase field)
    {
        if (!CanApplyCacheControlDirective(field))
        {
            return;
        }

        ITypeReference? typeRef = field.Type;

        if (!_typeLookup.TryNormalizeReference(typeRef!, out typeRef) ||
            !_typeRegistry.TryGetType(typeRef, out RegisteredType? registeredType))
        {
            // todo: maybe throw error
            return;
        }

        if (HasCacheControlDirective(registeredType.Type))
        {
            return;
        }

        if (registeredType.Kind == TypeKind.Scalar)
        {
            ApplyCacheControlWithInheritMaxAge(field);
        }
    }

    private void ApplyCacheControlWithDefaultMaxAge(OutputFieldDefinitionBase field)
    {
        field.Directives.Add(
            new DirectiveDefinition(
                new DirectiveNode(
                    "cacheControl",
                    new ArgumentNode("maxAge", _cacheControlOptions.DefaultMaxAge))));
    }

    private static void ApplyCacheControlWithInheritMaxAge(OutputFieldDefinitionBase field)
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

    private static bool CanApplyCacheControlDirective(OutputFieldDefinitionBase field)
    {
        IReadOnlyList<DirectiveDefinition> directives = field.GetDirectives();
        return directives.Count == 0 || !directives.Any(IsCacheControlDirective);
    }

    private static bool HasCacheControlDirective(ITypeSystemObject typeSystemObject)
    {
        if (typeSystemObject is not Language.IHasDirectives type)
        {
            return false;
        }

        return type.Directives.Any(IsCacheControlDirective);
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

    private static bool IsCacheControlDirective(DirectiveNode directive)
    {
        // todo: implement
        return true;
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