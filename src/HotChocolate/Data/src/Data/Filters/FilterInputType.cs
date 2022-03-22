using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Internal.FieldInitHelper;

namespace HotChocolate.Data.Filters;

public class FilterInputType
    : InputObjectType
    , IFilterInputType
{
    private Action<IFilterInputTypeDescriptor>? _configure;

    public FilterInputType()
    {
        _configure = Configure;
    }

    public FilterInputType(Action<IFilterInputTypeDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    public IExtendedType EntityType { get; private set; } = default!;

    protected override InputObjectTypeDefinition CreateDefinition(
        ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = FilterInputTypeDescriptor
                    .FromSchemaType(context.DescriptorContext, GetType(), context.Scope);
                _configure!(descriptor);
                Definition = descriptor.CreateDefinition();
            }

            return Definition;
        }
        finally
        {
            _configure = null;
        }
    }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        InputObjectTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);
        if (definition is FilterInputTypeDefinition { EntityType: { } } filterDefinition)
        {
            SetTypeIdentity(typeof(FilterInputType<>)
                .MakeGenericType(filterDefinition.EntityType));
        }
    }

    protected virtual void Configure(IFilterInputTypeDescriptor descriptor)
    {
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InputObjectTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);

        if (definition is FilterInputTypeDefinition ft &&
            ft.EntityType is { })
        {
            EntityType = context.TypeInspector.GetType(ft.EntityType);
        }
    }

    protected override FieldCollection<InputField> OnCompleteFields(
        ITypeCompletionContext context,
        InputObjectTypeDefinition definition)
    {
        var fields = new InputField[definition.Fields.Count + 2];
        var index = 0;

        if (definition is FilterInputTypeDefinition { UseAnd: true } def)
        {
            fields[index] = new AndField(context.DescriptorContext, index, def.Scope);
            index++;
        }

        if (definition is FilterInputTypeDefinition { UseOr: true } defOr)
        {
            fields[index] = new OrField(context.DescriptorContext, index, defOr.Scope);
            index++;
        }

        foreach (InputFieldDefinition fieldDefinition in
            definition.Fields.Where(t => !t.Ignore))
        {
            switch (fieldDefinition)
            {
                case FilterOperationFieldDefinition operation:
                    fields[index] = new FilterOperationField(operation);
                    index++;
                    break;

                case FilterFieldDefinition field:
                    fields[index] = new FilterField(field);
                    index++;
                    break;
            }
        }

        if (fields.Length > index)
        {
            Array.Resize(ref fields, index);
        }

        return CompleteFields(context, this, fields);
    }

    // we are disabling the default configure method so
    // that this does not lead to confusion.
    protected sealed override void Configure(
        IInputObjectTypeDescriptor descriptor)
    {
        throw new NotSupportedException();
    }

    internal FilterInputType(
        IDictionary<ITypeSystemMember, FilterInputTypeDefinition> definitionLookup,
        FilterInputTypeDefinition? explicitDefinition,
        ITypeReference filterType,
        ITypeReference filterOperationType,
        FilterFieldDefinition fieldDefinition)
    {
        if (filterType is null)
        {
            throw new ArgumentNullException(nameof(filterType));
        }

        FilterInputTypeDefinition filterTypeDefinition = explicitDefinition ?? new()
        {
            EntityType = typeof(object)
        };

        Definition = filterTypeDefinition;
        Definition.Dependencies.Add(new(filterType));
        Definition.Dependencies.Add(new(filterOperationType));
        Definition.NeedsNameCompletion = true;
        Definition.Configurations.Add(
            new CompleteConfiguration<FilterInputTypeDefinition>(
                CreateNamingConfiguration,
                filterTypeDefinition,
                ApplyConfigurationOn.Naming,
                new TypeDependency[] {
                    new(filterOperationType, TypeDependencyKind.Completed) ,
                    new(filterType, TypeDependencyKind.Completed)
                }));
        Definition.Configurations.Add(
            new CompleteConfiguration<FilterInputTypeDefinition>(
                CreateOperationFieldConfiguration,
                filterTypeDefinition,
                ApplyConfigurationOn.Completion,
                new TypeDependency[] {
                    new(filterOperationType, TypeDependencyKind.Completed),
                    new(filterType, TypeDependencyKind.Completed)
                }
            ));

        /// <summary>
        /// Creates the configuration for the naming process of the inline filter types.
        /// It uses the parent type name and the name of the field on which the new is applied,
        /// to create a new typename
        /// <example>
        /// ParentTypeName: AuthorFilterInputType
        /// Field: friends
        /// Result: AuthorFriendsFilterInputType
        /// </example>
        /// </summary>
        void CreateNamingConfiguration(
            ITypeCompletionContext context,
            FilterInputTypeDefinition definition)
        {
            if (definition.Name.HasValue)
            {
                return;
            }

            IFilterInputType parentFilterType = context.GetType<IFilterInputType>(filterType);
            IFilterConvention convention = context.GetFilterConvention(context.Scope);
            definition.Name = convention.GetTypeName(parentFilterType, fieldDefinition);
        }

        /// <summary>
        /// This configuration copies over the operations of the actual operation filter input to
        /// the new one with the subset of the operations
        /// </summary>
        void CreateOperationFieldConfiguration(
            ITypeCompletionContext context,
            FilterInputTypeDefinition definition)
        {
            IFilterInputType sourceType =
                context.GetType<IFilterInputType>(filterOperationType);

            if (!definitionLookup.TryGetValue(sourceType, out var typeDefinition))
            {
                throw ThrowHelper
                    .Filtering_DefinitionForTypeNotFound(definition, fieldDefinition, sourceType);
            }

            // if a field defines allowed operations, we copy over all the operations
            // of the original type
            if (fieldDefinition.HasAllowedOperations)
            {
                var operationFieldDefinitions = typeDefinition
                    .Fields
                    .OfType<FilterOperationFieldDefinition>()
                    .Where(x => fieldDefinition.AllowedOperations.Contains(x.Id));

                definition.Fields.AddRange(operationFieldDefinitions);

                if (fieldDefinition.AllowedOperations.Contains(DefaultFilterOperations.And))
                {
                    definition.UseAnd = true;
                }

                if (fieldDefinition.AllowedOperations.Contains(DefaultFilterOperations.Or))
                {
                    definition.UseOr = true;
                }
            }

            // in case there are no fields defined, the original type has either no fields
            // or only operation fields but no 'allows'. We report an error to the user that
            // this does not make sense
            if (definition.Fields.Count == 0 && definition is { UseAnd: false, UseOr: false })
            {
                IFilterInputType parentType = context.GetType<IFilterInputType>(filterType);
                context.ReportError(
                    ErrorHelper.Filtering_InlineFilterTypeHadNoFields(
                        definition,
                        context.Type,
                        fieldDefinition,
                        parentType));
            }

            /// we copy over the entity type of the type source.
            definition.EntityType = sourceType.EntityType?.Source;
        }
    }
}
