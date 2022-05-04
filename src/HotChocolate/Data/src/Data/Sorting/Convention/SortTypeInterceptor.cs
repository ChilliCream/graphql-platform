using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting;

public class SortTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<string, ISortConvention> _conventions = new();
    private readonly List<Func<ITypeReference>> _typesToRegister = new();
    private TypeRegistry _typeRegistry = default!;
    private readonly Dictionary<ITypeSystemMember, SortInputTypeDefinition> _definitions = new();

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
        switch (definition)
        {
            case SortInputTypeDefinition inputDefinition:
                OnBeforeRegisteringDependencies(discoveryContext, inputDefinition);
                break;
            case SortEnumTypeDefinition enumTypeDefinition:
                OnBeforeRegisteringDependencies(discoveryContext, enumTypeDefinition);
                break;
        }
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        switch (definition)
        {
            case SortInputTypeDefinition inputDefinition:
                OnBeforeCompleteName(completionContext, inputDefinition);
                break;
            case SortEnumTypeDefinition enumTypeDefinition:
                OnBeforeCompleteName(completionContext, enumTypeDefinition);
                break;
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        switch (definition)
        {
            case SortInputTypeDefinition inputDefinition:
                OnBeforeCompleteType(completionContext, inputDefinition);
                break;
            case SortEnumTypeDefinition enumTypeDefinition:
                OnBeforeCompleteType(completionContext, enumTypeDefinition);
                break;
        }
    }

    private void OnBeforeRegisteringDependencies(
        ITypeDiscoveryContext discoveryContext,
        SortInputTypeDefinition definition)
    {
        ISortConvention convention =
            GetConvention(discoveryContext.DescriptorContext, definition.Scope);

        _definitions[discoveryContext.Type] = definition;

        var descriptor = SortInputTypeDescriptor.New(
            discoveryContext.DescriptorContext,
            definition.EntityType!,
            definition.Scope);

        SchemaTypeReference typeReference =
            TypeReference.Create( discoveryContext.Type, definition.Scope);

        convention.ApplyConfigurations(typeReference, descriptor);

        SortInputTypeDefinition extensionDefinition = descriptor.CreateDefinition();

        discoveryContext.RegisterDependencies(extensionDefinition);

        foreach (InputFieldDefinition field in definition.Fields)
        {
            if (field is SortFieldDefinition sortField)
            {
                RegisterDynamicTypeConfiguration(
                    discoveryContext,
                    typeReference,
                    definition,
                    sortField);
            }
        }
    }

    private void OnBeforeRegisteringDependencies(
        ITypeDiscoveryContext discoveryContext,
        SortEnumTypeDefinition definition)
    {
        ISortConvention convention =
            GetConvention(discoveryContext.DescriptorContext, definition.Scope);

        var descriptor = SortEnumTypeDescriptor.New(
            discoveryContext.DescriptorContext,
            definition.EntityType,
            definition.Scope);

        SchemaTypeReference typeReference =
            TypeReference.Create(discoveryContext.Type, definition.Scope);

        convention.ApplyConfigurations(typeReference, descriptor);

        SortEnumTypeDefinition extensionDefinition = descriptor.CreateDefinition();

        discoveryContext.RegisterDependencies(extensionDefinition);
    }

    private void RegisterDynamicTypeConfiguration(
        ITypeDiscoveryContext discoveryContext,
        ITypeReference typeReference,
        SortInputTypeDefinition parentTypeDefinition,
        SortFieldDefinition sortField)
    {
        if (sortField.CreateFieldTypeDefinition is null)
        {
            return;
        }

        ITypeReference? originalType = null;
        _typesToRegister.Add(() =>
        {
            originalType = sortField.Type;
            sortField.Type = TypeReference.Create(
                $"SortSubTypeConfiguration_{Guid.NewGuid():N}",
                typeReference,
                Factory,
                TypeContext.Input);

            return sortField.Type;

            TypeSystemObjectBase Factory(IDescriptorContext _)
            {
                SortInputTypeDefinition? explicitDefinition = null;

                if (sortField.CreateFieldTypeDefinition is { } factory)
                {
                    explicitDefinition =
                        factory(discoveryContext.DescriptorContext, discoveryContext.Scope);
                }

                if (originalType is null ||
                    !_typeRegistry.TryGetType(originalType, out RegisteredType? registeredType))
                {
                    throw ThrowHelper.Sorting_FieldHadNoType(
                        sortField.Name.Value,
                        parentTypeDefinition.Name.Value);
                }

                if (!_definitions.TryGetValue(
                        registeredType.Type,
                        out SortInputTypeDefinition? definition))
                {
                    throw ThrowHelper.Sorting_DefinitionForTypeNotFound(
                        sortField.Name.Value,
                        parentTypeDefinition.Name.Value,
                        registeredType.Type.Name);
                }

                return new SortInputType(
                    definition,
                    explicitDefinition,
                    typeReference,
                    originalType!,
                    sortField);
            }
        });
    }

    private void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        SortInputTypeDefinition definition)
    {
        ISortConvention convention =
            GetConvention(completionContext.DescriptorContext, definition.Scope);

        var descriptor = SortInputTypeDescriptor.New(
            completionContext.DescriptorContext,
            definition.EntityType!,
            definition.Scope);

        SchemaTypeReference typeReference =
            TypeReference.Create(completionContext.Type, definition.Scope);

        convention.ApplyConfigurations(typeReference, descriptor);

        DataTypeExtensionHelper.MergeSortInputTypeDefinitions(
            completionContext,
            descriptor.CreateDefinition(),
            definition);

        if (definition is {Name: {HasValue: true}} and IHasScope {Scope: { }})
        {
            definition.Name = completionContext.Scope +
                "_" +
                definition.Name;
        }
    }

    private void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        SortEnumTypeDefinition definition)
    {
        ISortConvention convention =
            GetConvention(completionContext.DescriptorContext, definition.Scope);

        var descriptor = SortEnumTypeDescriptor.New(
            completionContext.DescriptorContext,
            definition.EntityType,
            definition.Scope);

        SchemaTypeReference typeReference =
            TypeReference.Create(completionContext.Type, definition.Scope);

        convention.ApplyConfigurations(typeReference, descriptor);

        DataTypeExtensionHelper.MergeSortEnumTypeDefinitions(
            completionContext,
            descriptor.CreateDefinition(),
            definition);

        if (definition is {Name: {HasValue: true}} and IHasScope {Scope: { }})
        {
            definition.Name = completionContext.Scope +
                "_" +
                definition.Name;
        }
    }

    private void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        SortInputTypeDefinition definition)
    {
        ISortConvention convention =
            GetConvention(completionContext.DescriptorContext, definition.Scope);

        foreach (InputFieldDefinition field in definition.Fields)
        {
            if (field is SortFieldDefinition sortFieldDefinition)
            {
                if (sortFieldDefinition.Type is null)
                {
                    throw ThrowHelper.Sorting_FieldHadNoType(field.Name, definition.Name);
                }

                if (completionContext.TryPredictTypeKind(
                        sortFieldDefinition.Type,
                        out TypeKind kind) &&
                    kind != TypeKind.Enum)
                {
                    field.Type = field.Type!.With(scope: completionContext.Scope);
                }

                sortFieldDefinition.Metadata =
                    convention.CreateMetaData(completionContext, definition, sortFieldDefinition);

                if (sortFieldDefinition.Handler is null)
                {
                    if (convention.TryGetFieldHandler(
                            completionContext,
                            definition,
                            sortFieldDefinition,
                            out ISortFieldHandler? handler))
                    {
                        sortFieldDefinition.Handler = handler;
                    }
                    else
                    {
                        throw ThrowHelper.SortInterceptor_NoFieldHandlerFoundForField(
                            definition,
                            sortFieldDefinition);
                    }
                }
            }
        }
    }

    private void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        SortEnumTypeDefinition definition)
    {
        ISortConvention convention =
            GetConvention(completionContext.DescriptorContext, completionContext.Scope);

        foreach (EnumValueDefinition? enumValue in definition.Values)
        {
            if (enumValue is SortEnumValueDefinition sortEnumValueDefinition)
            {
                if (convention.TryGetOperationHandler(
                        completionContext,
                        definition,
                        sortEnumValueDefinition,
                        out ISortOperationHandler? handler))
                {
                    sortEnumValueDefinition.Handler = handler;
                }
                else
                {
                    throw ThrowHelper.SortInterceptor_NoOperationHandlerFoundForValue(
                        definition,
                        sortEnumValueDefinition);
                }
            }
        }
    }

    private ISortConvention GetConvention(IDescriptorContext context, string? scope)
    {
        if (!_conventions.TryGetValue(scope ?? string.Empty, out ISortConvention? convention))
        {
            convention = context.GetSortConvention(scope);
            _conventions[scope ?? string.Empty] = convention;
        }

        return convention;
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
}
