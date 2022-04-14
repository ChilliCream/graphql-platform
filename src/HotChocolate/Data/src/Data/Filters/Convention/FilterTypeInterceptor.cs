using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Filters;

public class FilterTypeInterceptor
    : TypeInterceptor
{
    private readonly Dictionary<string, IFilterConvention> _conventions = new();
    private readonly Dictionary<ITypeSystemMember, FilterInputTypeDefinition> _definitions = new();
    private readonly List<Func<ITypeReference>> _typesToRegister = new();
    private TypeRegistry _typeRegistry = default!;

    public override bool CanHandle(ITypeSystemObjectContext context) => true;

    public override bool TriggerAggregations => true;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _typeRegistry = typeRegistry;
    }

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (definition is not FilterInputTypeDefinition { EntityType: { } } def)
        {
            return;
        }
        _definitions[discoveryContext.Type] = def;

        IFilterConvention convention =
            GetConvention(discoveryContext.DescriptorContext, def.Scope);

        SchemaTypeReference typeReference =
            TypeReference.Create(discoveryContext.Type, def.Scope);

        var descriptor = FilterInputTypeDescriptor
            .New(discoveryContext.DescriptorContext, def.EntityType, def.Scope);

        ApplyCorrectScope(def, discoveryContext);

        convention.ApplyConfigurations(typeReference, descriptor);

        FilterInputTypeDefinition extensionDefinition = descriptor.CreateDefinition();

        ApplyCorrectScope(extensionDefinition, discoveryContext);

        discoveryContext.RegisterDependencies(extensionDefinition);

        foreach (InputFieldDefinition field in def.Fields)
        {
            if (field is FilterFieldDefinition filterField)
            {
                if (filterField.Member?.GetCustomAttribute(typeof(IDAttribute)) != null)
                {
                    filterField.Type = discoveryContext.TypeInspector.GetTypeRef(
                        typeof(IdOperationFilterInputType),
                        TypeContext.Input,
                        discoveryContext.Scope);
                }

                RegisterDynamicTypeConfiguration(discoveryContext, typeReference, def, filterField);
            }
        }
    }

    private void RegisterDynamicTypeConfiguration(
        ITypeDiscoveryContext discoveryContext,
        ITypeReference typeReference,
        FilterInputTypeDefinition parentTypeDefinition,
        FilterFieldDefinition filterField)
    {
        if (!filterField.HasAllowedOperations &&
            filterField.CreateFieldTypeDefinition is null)
        {
            return;
        }

        ITypeReference? originalType = null;
        _typesToRegister.Add(() =>
        {
            originalType = filterField.Type;
            filterField.Type = TypeReference.Create(
                $"FilterSubTypeConfiguration_{Guid.NewGuid():N}",
                typeReference,
                Factory,
                TypeContext.Input);

            return filterField.Type;

            TypeSystemObjectBase Factory(IDescriptorContext _)
            {
                FilterInputTypeDefinition? explicitDefinition = null;

                if (filterField.CreateFieldTypeDefinition is { } factory)
                {
                    explicitDefinition =
                        factory(discoveryContext.DescriptorContext, discoveryContext.Scope);
                }

                if (originalType is null ||
                    !_typeRegistry.TryGetType(originalType, out RegisteredType? registeredType))
                {
                    throw ThrowHelper.Filtering_FieldHadNoType(
                            filterField.Name.Value,
                            parentTypeDefinition.Name.Value);
                }

                if (!_definitions.TryGetValue(
                        registeredType.Type,
                        out FilterInputTypeDefinition? definition))
                {
                    throw ThrowHelper.Filtering_DefinitionForTypeNotFound(
                            filterField.Name.Value,
                            parentTypeDefinition.Name.Value,
                            registeredType.Type.Name);
                }

                return new FilterInputType(
                    definition,
                    explicitDefinition,
                    typeReference,
                    originalType!,
                    filterField);
            }
        });
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (definition is not FilterInputTypeDefinition def)
        {
            return;
        }

        IFilterConvention convention =
            GetConvention(completionContext.DescriptorContext, def.Scope);

        var descriptor = FilterInputTypeDescriptor
            .New(completionContext.DescriptorContext, def.EntityType!, def.Scope);

        SchemaTypeReference typeReference =
            TypeReference.Create(completionContext.Type, def.Scope);

        convention.ApplyConfigurations(typeReference, descriptor);

        DataTypeExtensionHelper
            .MergeFilterInputTypeDefinitions(completionContext, descriptor.CreateDefinition(), def);

        if (def.Scope is not null)
        {
            definition.Name = completionContext.Scope + "_" + definition.Name;
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (definition is not FilterInputTypeDefinition { EntityType: { } } def)
        {
            return;
        }

        IFilterConvention convention =
            GetConvention(completionContext.DescriptorContext, def.Scope);

        foreach (InputFieldDefinition field in def.Fields)
        {
            if (field is FilterFieldDefinition filterFieldDefinition)
            {
                if (filterFieldDefinition.Type is null)
                {
                    throw ThrowHelper
                        .FilterInterceptor_OperationHasNoTypeSpecified(def, filterFieldDefinition);
                }

                if (filterFieldDefinition.Handler is null)
                {
                    if (convention.TryGetHandler(
                        completionContext,
                        def,
                        filterFieldDefinition,
                        out IFilterFieldHandler? handler))
                    {
                        filterFieldDefinition.Handler = handler;
                    }
                    else
                    {
                        throw ThrowHelper
                            .FilterInterceptor_NoHandlerFoundForField(def, filterFieldDefinition);
                    }
                }
            }
        }
    }

    public override IEnumerable<ITypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        if (_typesToRegister.Count == 0)
        {
            return Array.Empty<ITypeReference>();
        }

        ITypeReference[] typesToRegister = _typesToRegister
            .Select(x => x())
            .ToArray();

        _typesToRegister.Clear();
        return typesToRegister;
    }

    private IFilterConvention GetConvention(IDescriptorContext context, string? scope)
    {
        if (!_conventions.TryGetValue(scope ?? "", out IFilterConvention? convention))
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
        foreach (InputFieldDefinition field in definition.Fields)
        {
            if (field is FilterFieldDefinition filterFieldDefinition &&
                field.Type is not null &&
                filterFieldDefinition.Type is { } filterFieldType &&
                discoveryContext.TryPredictTypeKind(filterFieldType, out TypeKind kind) &&
                kind is not TypeKind.Scalar and not TypeKind.Enum)
            {
                field.Type = field.Type.With(scope: discoveryContext.Scope);
            }
        }
    }
}
