using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Relay;
using static HotChocolate.Data.Filters.FilterInputTypeDescriptor;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Filters;

public sealed class FilterTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<string, IFilterConvention> _conventions = new();

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        if (definition is not FilterInputTypeDefinition { EntityType: { }, } def)
        {
            return;
        }

        var convention = GetConvention(discoveryContext.DescriptorContext, def.Scope);
        var typeReference = TypeReference.Create(discoveryContext.Type, def.Scope);
        var descriptor = New(discoveryContext.DescriptorContext, def.EntityType!, def.Scope);

        ApplyCorrectScope(def, discoveryContext);

        convention.ApplyConfigurations(typeReference, descriptor);

        var extensionDefinition = descriptor.CreateDefinition();

        ApplyCorrectScope(extensionDefinition, discoveryContext);

        discoveryContext.RegisterDependencies(extensionDefinition);

        ApplyIdAttributesToFields(discoveryContext, def);
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is not FilterInputTypeDefinition def)
        {
            return;
        }

        var convention =
            GetConvention(completionContext.DescriptorContext, def.Scope);

        var descriptor = New(completionContext.DescriptorContext, def.EntityType!, def.Scope);

        var typeReference =
            TypeReference.Create(completionContext.Type, def.Scope);

        convention.ApplyConfigurations(typeReference, descriptor);

        DataTypeExtensionHelper
            .MergeFilterInputTypeDefinitions(completionContext, descriptor.CreateDefinition(), def);

        if (def.Scope is not null)
        {
            definition.Name = $"{completionContext.Scope}_{definition.Name}";
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is not FilterInputTypeDefinition { EntityType: { }, } def)
        {
            return;
        }

        var convention = GetConvention(completionContext.DescriptorContext, def.Scope);

        foreach (var field in def.Fields)
        {
            if (field is FilterFieldDefinition filterFieldDefinition)
            {
                if (filterFieldDefinition.Type is null)
                {
                    throw FilterInterceptor_OperationHasNoTypeSpecified(def, filterFieldDefinition);
                }

                if (filterFieldDefinition.Handler is null)
                {
                    if (convention.TryGetHandler(
                            completionContext,
                            def,
                            filterFieldDefinition,
                            out var handler))
                    {
                        filterFieldDefinition.Handler = handler;
                    }

                    filterFieldDefinition.Metadata =
                        convention.CreateMetaData(completionContext, def, filterFieldDefinition);

                    if (filterFieldDefinition.Handler is null)
                    {
                        throw FilterInterceptor_NoHandlerFoundForField(def, filterFieldDefinition);
                    }
                }
            }
        }
    }

    private IFilterConvention GetConvention(IDescriptorContext context, string? scope)
    {
        if (!_conventions.TryGetValue(scope ?? "", out var convention))
        {
            convention = context.GetFilterConvention(scope);
            _conventions[scope ?? ""] = convention;
        }

        return convention;
    }

    private static void ApplyCorrectScope(
        InputObjectTypeDefinition definition,
        ITypeDiscoveryContext discoveryContext)
    {
        foreach (var field in definition.Fields)
        {
            if (field is FilterFieldDefinition filterFieldDefinition &&
                field.Type is not null &&
                filterFieldDefinition.Type is { } filterFieldType &&
                discoveryContext.TryPredictTypeKind(filterFieldType, out var kind) &&
                kind is not TypeKind.Scalar and not TypeKind.Enum)
            {
                field.Type = field.Type.With(scope: discoveryContext.Scope);
            }
        }
    }

    private static void ApplyIdAttributesToFields(
        ITypeDiscoveryContext discoveryContext,
        FilterInputTypeDefinition def)
    {
        foreach (var field in def.Fields)
        {
            if (field.HasIdAttribute())
            {
                field.Type = discoveryContext.TypeInspector.GetTypeRef(
                    typeof(IdOperationFilterInputType),
                    TypeContext.Input,
                    discoveryContext.Scope);
            }
        }
    }
}

file static class Extensions
{
    public static bool HasIdAttribute(this InputFieldDefinition? definition)
    {
        if (definition is not FilterFieldDefinition { Member: { } member })
        {
            return false;
        }

        var attributes = member.GetCustomAttributesData();
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeType == typeof(IDAttribute))
            {
                return true;
            }

            if (attribute.AttributeType.IsGenericType &&
                attribute.AttributeType.GetGenericTypeDefinition() == typeof(IDAttribute<>))
            {
                return true;
            }
        }

        return false;
    }
}
