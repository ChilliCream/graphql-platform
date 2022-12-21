using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Internal.FieldInitHelper;

namespace HotChocolate.Data.Sorting;

public class SortInputType
    : InputObjectType
    , ISortInputType
{
    private Action<ISortInputTypeDescriptor>? _configure;

    public SortInputType()
    {
        _configure = Configure;
    }

    public SortInputType(Action<ISortInputTypeDescriptor> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    public IExtendedType EntityType { get; private set; } = default!;

    protected override InputObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        if (Definition is null)
        {
            var descriptor = SortInputTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType(),
                context.Scope);

            _configure!(descriptor);
            _configure = null;

            Definition = descriptor.CreateDefinition();
        }

        return Definition;
    }

    protected virtual void Configure(ISortInputTypeDescriptor descriptor)
    {
    }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        InputObjectTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);
        if (definition is SortInputTypeDefinition {EntityType: { }} sortDefinition)
        {
            SetTypeIdentity(
                typeof(SortInputType<>).MakeGenericType(sortDefinition.EntityType));
        }
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        InputObjectTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);

        if (definition is SortInputTypeDefinition { EntityType: not null } ft)
        {
            EntityType = context.TypeInspector.GetType(ft.EntityType);
        }
    }

    protected override FieldCollection<InputField> OnCompleteFields(
        ITypeCompletionContext context,
        InputObjectTypeDefinition definition)
    {
        var fields = new InputField[definition.Fields.Count];
        var index = 0;

        foreach (var fieldDefinition in definition.Fields)
        {
            if (fieldDefinition is SortFieldDefinition {Ignore: false} field)
            {
                fields[index] = new SortField(field, index);
                index++;
            }
        }

        if (fields.Length < index)
        {
            Array.Resize(ref fields, index);
        }

        return CompleteFields(context, this, fields);
    }

    // we are disabling the default configure method so
    // that this does not lead to confusion.
    protected sealed override void Configure(IInputObjectTypeDescriptor descriptor)
    {
        throw new NotSupportedException();
    }

    internal SortInputType(
        SortInputTypeDefinition sourceTypeDefinition,
        SortInputTypeDefinition? explicitDefinition,
        ITypeReference sortType,
        ITypeReference sortOperationType,
        SortFieldDefinition fieldDefinition)
    {
        if (sortType is null)
        {
            throw new ArgumentNullException(nameof(sortType));
        }

        var sortTypeDefinition = explicitDefinition ?? new()
        {
            EntityType = typeof(object)
        };

        // we merge/copy the operation fields from the original type. This way users do not have
        // to declare the types of operations
        foreach (var field in sourceTypeDefinition.Fields.OfType<SortFieldDefinition>())
        {
            var userDefinedField = sortTypeDefinition.Fields
                .OfType<SortFieldDefinition>()
                .FirstOrDefault(x => x.Name == field.Name);

            if (userDefinedField is not null)
            {
                SortFieldDefinition newDefinition = new();
                field.CopyTo(newDefinition);
                userDefinedField.MergeInto(newDefinition);
                sortTypeDefinition.Fields.Remove(userDefinedField);
                sortTypeDefinition.Fields.Add(newDefinition);
            }
        }

        Definition = sortTypeDefinition;
        Definition.Dependencies.Add(new(sortType));
        Definition.Dependencies.Add(new(sortOperationType));
        Definition.NeedsNameCompletion = true;
        Definition.Configurations.Add(
            new CompleteConfiguration<SortInputTypeDefinition>(
                CreateNamingConfiguration,
                sortTypeDefinition,
                ApplyConfigurationOn.BeforeNaming,
                new TypeDependency[]
                {
                    new(sortOperationType, TypeDependencyFulfilled.Named),
                    new(sortType, TypeDependencyFulfilled.Named)
                }));

        // <summary>
        // Creates the configuration for the naming process of the inline sort types.
        // It uses the parent type name and the name of the field on which the new is applied,
        // to create a new typename
        // <example>
        // ParentTypeName: AuthorSortInputType
        // Field: friends
        // Result: AuthorFriendsSortInputType
        // </example>
        // </summary>
        void CreateNamingConfiguration(
            ITypeCompletionContext context,
            SortInputTypeDefinition definition)
        {
            if (definition.IsNamed)
            {
                return;
            }

            var parentSortType = context.GetType<ISortInputType>(sortType);
            var convention = context.GetSortConvention(context.Scope);
            definition.Name = convention.GetTypeName(parentSortType, fieldDefinition);
        }
    }
}
