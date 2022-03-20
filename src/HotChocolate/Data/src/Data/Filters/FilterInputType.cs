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
        ITypeReference filterType,
        ITypeReference filterOperationType,
        FilterFieldDefinition fieldDefinition)
    {
        if (filterType is null)
        {
            throw new ArgumentNullException(nameof(filterType));
        }

        var inputTypeDefinition = new FilterInputTypeDefinition();
        Definition = inputTypeDefinition;
        Definition.Dependencies.Add(new(filterType));
        Definition.Dependencies.Add(new(filterOperationType));
        Definition.NeedsNameCompletion = true;
        Definition.Configurations.Add(
            new CompleteConfiguration<FilterInputTypeDefinition>(
                (c, definition) =>
                {
                    // TODO const => FilterContenction.cs
                    const string inputPostFix = "FilterInput";
                    const string _inputTypePostFix = "FilterInputType";

                    IFilterInputType operationType = c.GetType<IFilterInputType>(filterOperationType);
                    IFilterInputType parentFilterType = c.GetType<IFilterInputType>(filterType);

                    // TODO this is probably not good
                    string parentName = parentFilterType.Name;
                    if (parentName.EndsWith(inputPostFix, StringComparison.Ordinal))
                    {
                        parentName = parentName.Remove(parentName.Length - inputPostFix.Length);
                    }
                    if (parentName.EndsWith(_inputTypePostFix, StringComparison.Ordinal))
                    {
                        parentName = parentName.Remove(parentName.Length - _inputTypePostFix.Length);
                    }
                    definition.Name =
                        // TODO conventions
                        parentName +
                        UppercaseFirstLetter(fieldDefinition.Name) +
                        operationType.Name;
                },
                inputTypeDefinition,
                ApplyConfigurationOn.Naming,
                new TypeDependency[] {
                    new(filterOperationType, TypeDependencyKind.Completed) ,
                    new(filterType, TypeDependencyKind.Completed)
                }));
        Definition.Configurations.Add(
            new CompleteConfiguration<FilterInputTypeDefinition>(
                (c, definition) =>
                {
                    IFilterInputType operationType = c.GetType<IFilterInputType>(filterOperationType);
                    if (!definitionLookup.TryGetValue(operationType, out var typeDefinition))
                    {
                        // TODO throwhelper
                        throw new Exception("");
                    }

                    var fieldDefinitions = typeDefinition
                        .Fields
                        .OfType<FilterOperationFieldDefinition>()
                        .Where(x => fieldDefinition.AllowedOperations.Contains(x.Id));

                    definition.EntityType = operationType.EntityType.Source;
                    definition.Fields.AddRange(fieldDefinitions);
                },
                inputTypeDefinition,
                ApplyConfigurationOn.Completion,
                filterOperationType,
                TypeDependencyKind.Completed
                ));
    }

    public static string UppercaseFirstLetter(string? s)
    {
        if (s is null)
        {
            throw new ArgumentNullException(nameof(s));
        }
        s = s.Trim();
        if (s.Length < 1)
        {
            throw new ArgumentException("Provided string was empty.", nameof(s));
        }

        return $"{char.ToUpper(s[0], CultureInfo.InvariantCulture)}{s.Substring(1)}";
    }
}
