using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Caching;

internal sealed class CacheControlTypeInterceptor : TypeInterceptor
{
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

        if (!context.ContextData.TryGetValue(WellKnownContextData.CacheControlOptions, out var options) ||
            options is not ICacheControlOptions typedOptions)
        {
            typedOptions = new CacheControlOptions();
        }

        _cacheControlOptions = typedOptions;
    }

    public override void OnValidateType(ITypeSystemObjectContext validationContext, DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (validationContext.Type is not ObjectType obj)
        {
            return;
        }

        foreach (ObjectField field in obj.Fields)
        {
            CacheControlDirective? directive = field.Directives.FirstOrDefault(d =>
                d.Name == "cacheControl")?.ToObject<CacheControlDirective>();

            if (directive is null)
            {
                continue;
            }

            if (directive.MaxAge.HasValue)
            {
                if (directive.MaxAge.Value < 0)
                {
                    // todo: error helper and more information about location
                    ISchemaError error = SchemaErrorBuilder.New()
                                .SetMessage("Value of `maxAge` on @cacheControl directive can not be negative.")
                                .Build();

                    validationContext.ReportError(error);
                }

                if (directive.InheritMaxAge == true)
                {
                    // todo: error helper and more information about location
                    ISchemaError error = SchemaErrorBuilder.New()
                                .SetMessage("@cacheControl directive can not specify `inheritMaxAge: true` and a value for `maxAge`.")
                                .Build();

                    validationContext.ReportError(error);
                }
            }
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

        if (registeredType.Kind != TypeKind.Scalar)
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
}