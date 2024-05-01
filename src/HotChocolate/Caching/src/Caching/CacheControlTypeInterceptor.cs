using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Caching;

internal sealed class CacheControlTypeInterceptor : TypeInterceptor
{
    private readonly List<(RegisteredType Type, ObjectTypeDefinition TypeDef)> _types = [];
    private readonly ICacheControlOptions _cacheControlOptions;
    private TypeDependency? _cacheControlDependency;

    public CacheControlTypeInterceptor(ICacheControlOptionsAccessor accessor)
    {
        _cacheControlOptions = accessor.CacheControl;
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (!_cacheControlOptions.Enable || !_cacheControlOptions.ApplyDefaults)
        {
            return;
        }

        if (completionContext.Type is ObjectType && definition is ObjectTypeDefinition typeDef)
        {
            _types.Add(((RegisteredType)completionContext, typeDef));
        }
    }

    public override void OnAfterMergeTypeExtensions()
    {
        foreach (var item in _types)
        {
            TryApplyDefaults(item.Type, item.TypeDef);
        }
    }

    private void TryApplyDefaults(RegisteredType type, ObjectTypeDefinition objectDef)
    {
        if (!_cacheControlOptions.Enable || !_cacheControlOptions.ApplyDefaults)
        {
            return;
        }

        if (type.IsIntrospectionType ||
            type.IsSubscriptionType == true ||
            type.IsMutationType == true)
        {
            return;
        }

        _cacheControlDependency ??= new TypeDependency(
            new ExtendedTypeDirectiveReference(
                type.TypeInspector.GetType(
                    typeof(CacheControlDirectiveType))),
            TypeDependencyFulfilled.Completed);

        var length = objectDef.Fields.Count;
        var appliedDefaults = false;

#if NET6_0_OR_GREATER
        var fields = ((BindableList<ObjectFieldDefinition>)objectDef.Fields).AsSpan();
#else
        var fields = (BindableList<ObjectFieldDefinition>)objectDef.Fields;
#endif

        for (var i = 0; i < length; i++)
        {
            var field = fields[i];

            if (field.IsIntrospectionField)
            {
                // Introspection fields do not need to be declared as cachable.
                continue;
            }

            if (HasCacheControlDirective(field))
            {
                // If the field has a @cacheControl directive,
                // we do not need to apply any defaults.
                continue;
            }

            if (type.IsQueryType == true || IsDataResolver(field))
            {
                // Each field on the query type or data resolver fields
                // are treated as fields that need to be explicitly cached.
                ApplyCacheControlWithDefaults(field);
                appliedDefaults = true;
            }
        }

        if (appliedDefaults)
        {
            type.Dependencies.Add(_cacheControlDependency);
        }
    }

    private void ApplyCacheControlWithDefaults(
        OutputFieldDefinitionBase field)
    {
        var isNotDefaultScope = _cacheControlOptions.DefaultScope != CacheControlDefaults.Scope;

        var arguments = new ArgumentNode[isNotDefaultScope ? 2 : 1];
        arguments[0] = new ArgumentNode(
            CacheControlDirectiveType.Names.MaxAgeArgName,
            _cacheControlOptions.DefaultMaxAge);

        if (isNotDefaultScope)
        {
            arguments[1] = new ArgumentNode(
                CacheControlDirectiveType.Names.ScopeArgName,
                new EnumValueNode(_cacheControlOptions.DefaultScope));
        }

        field.Directives.Add(
            new DirectiveDefinition(
                new DirectiveNode(
                    CacheControlDirectiveType.Names.DirectiveName,
                    arguments)));
    }

    private static bool HasCacheControlDirective(ObjectFieldDefinition field)
        => field.Directives.Any(static d => IsCacheControlDirective(d));

    private static bool IsCacheControlDirective(DirectiveDefinition directive)
    {
        if (directive.Type is NameDirectiveReference directiveReference &&
            directiveReference.Name.EqualsOrdinal(CacheControlDirectiveType.Names.DirectiveName))
        {
            return true;
        }

        if (directive.Type is ExtendedTypeDirectiveReference { Type.Type: { } type, } &&
            type == typeof(CacheControlDirective))
        {
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Defines if a resolver is possible fetching data and causing higher impact on the system.
    /// </summary>
    internal static bool IsDataResolver(ObjectFieldDefinition field)
    {
        if (field.PureResolver is not null && field.MiddlewareDefinitions.Count == 0)
        {
            return false;
        }

        if (field.Resolver is not null)
        {
            return true;
        }

        var resolver = field.ResolverMember ?? field.Member;

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
}
