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
using static HotChocolate.Data.Filters.FilterInputTypeDescriptor;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Filters;

public sealed class FilterTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<string, IFilterConvention> _conventions = new();
    private readonly Dictionary<ITypeSystemMember, FilterInputTypeDefinition> _definitions = new();
    private readonly List<Func<ITypeReference>> _typesToRegister = new();
    private TypeRegistry _typeRegistry = default!;

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
        DefinitionBase definition)
    {
        if (definition is not FilterInputTypeDefinition { EntityType: { } } def)
        {
            return;
        }

        _definitions[discoveryContext.Type] = def;

        var convention = GetConvention(discoveryContext.DescriptorContext, def.Scope);
        var typeReference = TypeReference.Create(discoveryContext.Type, def.Scope);
        var descriptor = New(discoveryContext.DescriptorContext, def.EntityType, def.Scope);

        ApplyCorrectScope(def, discoveryContext);

        convention.ApplyConfigurations(typeReference, descriptor);

        var extensionDefinition = descriptor.CreateDefinition();

        ApplyCorrectScope(extensionDefinition, discoveryContext);

        discoveryContext.RegisterDependencies(extensionDefinition);

        foreach (var field in def.Fields)
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

        ITypeReference? originalType;
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
                    !_typeRegistry.TryGetType(originalType, out var registeredType))
                {
                    throw Filtering_FieldHadNoType(filterField.Name, parentTypeDefinition.Name);
                }

                if (!_definitions.TryGetValue(
                        registeredType.Type,
                        out var definition))
                {
                    throw Filtering_DefinitionForTypeNotFound(
                        filterField.Name,
                        parentTypeDefinition.Name,
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
            definition.Name = completionContext.Scope + "_" + definition.Name;
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is not FilterInputTypeDefinition { EntityType: { } } def)
        {
            return;
        }

        var convention =
            GetConvention(completionContext.DescriptorContext, def.Scope);

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

    public override IEnumerable<ITypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        if (_typesToRegister.Count == 0)
        {
            return Array.Empty<ITypeReference>();
        }

        var typesToRegister = _typesToRegister
            .Select(x => x())
            .ToArray();

        _typesToRegister.Clear();
        return typesToRegister;
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
}
