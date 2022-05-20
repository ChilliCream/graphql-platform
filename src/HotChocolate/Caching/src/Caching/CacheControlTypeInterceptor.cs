using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Caching;

internal sealed class CacheControlTypeInterceptor : TypeInterceptor
{
    private ICacheControlOptions _cacheControlOptions = default!;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        if (!context.ContextData.TryGetValue(WellKnownContextData.CacheControlOptions,
                out var options) || options is not ICacheControlOptions typedOptions)
        {
            typedOptions = new CacheControlOptions();
        }

        _cacheControlOptions = typedOptions;
    }

    public override void OnValidateType(ITypeSystemObjectContext validationContext,
        DefinitionBase? definition, IDictionary<string, object?> contextData)
    {
        if (validationContext.Type is not ObjectType obj)
        {
            return;
        }

        foreach (ObjectField field in obj.Fields)
        {
            CacheControlDirective? directive = field.Directives
                .FirstOrDefault(d => d.Name == CacheControlDirectiveType.DirectiveName)
                ?.ToObject<CacheControlDirective>();

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

        if (definition is not ObjectTypeDefinition objectDef)
        {
            return;
        }

        foreach (ObjectFieldDefinition field in objectDef.Fields)
        {
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

            if (completionContext.IsQueryType == true ||
                CostTypeInterceptor.IsDataResolver(field))
            {
                // Each field on the query type or data resolver fields
                // are treated as fields that need to be explicitly cached.
                ApplyCacheControlWithDefaultMaxAge(field);
            }
        }
    }

    private void ApplyCacheControlWithDefaultMaxAge(OutputFieldDefinitionBase field)
    {
        field.Directives.Add(
            new DirectiveDefinition(
                new DirectiveNode(
                    CacheControlDirectiveType.DirectiveName,
                    new ArgumentNode("maxAge", _cacheControlOptions.DefaultMaxAge))));
    }

    private static bool HasCacheControlDirective(ObjectFieldDefinition field)
    {
        return field.Directives.Any(IsCacheControlDirective);
    }

    private static bool IsCacheControlDirective(DirectiveDefinition directive)
    {
        if (directive.Reference is NameDirectiveReference directiveReference &&
            directiveReference.Name.Value == CacheControlDirectiveType.DirectiveName)
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
}
