using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Caching;

internal class CacheControlTypeInterceptor : TypeInterceptor
{
    private bool _optionsResolved;
    private ICacheControlOptions _options = default!;

    public override void OnBeforeRegisterDependencies(
       ITypeDiscoveryContext discoveryContext,
       DefinitionBase? definition,
       IDictionary<string, object?> contextData)
    {
        EnsureCacheControlSettingsAreLoaded(discoveryContext.DescriptorContext);

        if (!_options.Enable)
        {
            return;
        }

        if (_options.ApplyDefaults &&
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
        if (!_options.Enable || !_options.ApplyDefaults)
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

            if (completionContext.IsQueryType == true || IsDataResolver(field))
            {
                ApplyDataResolverCacheControl(field);
            }
            else
            {
                ApplyPureResolverCacheControl(field);
            }
        }
    }

    private void ApplyDataResolverCacheControl(ObjectFieldDefinition field)
    {
        field.Directives.Add(
            new DirectiveDefinition(
                new DirectiveNode(
                    "cacheControl",
                    new ArgumentNode("maxAge", _options.DefaultMaxAge))));
    }

    private static void ApplyPureResolverCacheControl(ObjectFieldDefinition field)
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
        if (!_optionsResolved)
        {
            // todo: load proper settings
            _options =
                descriptorContext.ContextData.TryGetValue("TODO", out var value) &&
                value is ICacheControlOptions cacheControlOptions
                    ? cacheControlOptions
                    : new CacheControlOptions();

            _optionsResolved = true;
        }
    }
}