using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters;

internal static class DataTypeExtensionHelper
{
    public static void MergeFilterInputTypeDefinitions(
        ITypeCompletionContext context,
        FilterInputTypeConfiguration extensionConfiguration,
        FilterInputTypeConfiguration typeConfiguration)
    {
        TypeExtensionHelper.MergeFeatures(
            extensionConfiguration,
            typeConfiguration);

        TypeExtensionHelper.MergeDirectives(
            context,
            extensionConfiguration.Directives,
            typeConfiguration.Directives);

        MergeFilterFieldDefinitions(
            context,
            extensionConfiguration.Fields,
            typeConfiguration.Fields);

        TypeExtensionHelper.MergeConfigurations(
            extensionConfiguration.Tasks,
            typeConfiguration.Tasks);
    }

    public static void MergeSortEnumTypeDefinitions(
        ITypeCompletionContext context,
        SortEnumTypeConfiguration extensionConfiguration,
        SortEnumTypeConfiguration typeConfiguration)
    {
        TypeExtensionHelper.MergeFeatures(
            extensionConfiguration,
            typeConfiguration);

        TypeExtensionHelper.MergeDirectives(
            context,
            extensionConfiguration.Directives,
            typeConfiguration.Directives);

        MergeSortEnumValueDefinitions(
            context,
            extensionConfiguration.Values,
            typeConfiguration.Values);

        TypeExtensionHelper.MergeConfigurations(
            extensionConfiguration.Tasks,
            typeConfiguration.Tasks);
    }

    public static void MergeSortInputTypeDefinitions(
        ITypeCompletionContext context,
        SortInputTypeConfiguration extensionConfiguration,
        SortInputTypeConfiguration typeConfiguration)
    {
        TypeExtensionHelper.MergeFeatures(
            extensionConfiguration,
            typeConfiguration);

        TypeExtensionHelper.MergeDirectives(
            context,
            extensionConfiguration.Directives,
            typeConfiguration.Directives);

        MergeSortInputFieldDefinitions(
            context,
            extensionConfiguration.Fields,
            typeConfiguration.Fields);

        TypeExtensionHelper.MergeConfigurations(
            extensionConfiguration.Tasks,
            typeConfiguration.Tasks);
    }

    private static void MergeFilterFieldDefinitions(
        ITypeCompletionContext context,
        IList<InputFieldConfiguration> extensionFields,
        IList<InputFieldConfiguration> typeFields)
    {
        MergeFilterFields(
            context,
            extensionFields,
            typeFields,
            (_, extensionField, typeField) =>
            {
                if (typeField is FilterFieldConfiguration filterTypeField
                    && extensionField is FilterFieldConfiguration filterExtensionField)
                {
                    filterTypeField.Handler ??= filterExtensionField.Handler;
                }

                typeField.Description ??= extensionField.Description;
                typeField.RuntimeDefaultValue ??= extensionField.RuntimeDefaultValue;
            });
    }

    private static void MergeSortEnumValueDefinitions(
        ITypeCompletionContext context,
        IList<EnumValueConfiguration> extensionFields,
        IList<EnumValueConfiguration> typeFields)
    {
        MergeSortEnumValues(
            context,
            extensionFields,
            typeFields,
            (_, extensionField, typeField) =>
            {
                if (typeField is SortEnumValueConfiguration filterTypeField
                    && extensionField is SortEnumValueConfiguration filterExtensionField)
                {
                    filterTypeField.Handler ??= filterExtensionField.Handler;
                }

                typeField.Description ??= extensionField.Description;
            });
    }

    private static void MergeSortInputFieldDefinitions(
        ITypeCompletionContext context,
        IList<InputFieldConfiguration> extensionFields,
        IList<InputFieldConfiguration> typeFields)
    {
        MergeSortFields(
            context,
            extensionFields,
            typeFields,
            (_, extensionField, typeField) =>
            {
                if (typeField is SortFieldConfiguration filterTypeField
                    && extensionField is SortFieldConfiguration filterExtensionField)
                {
                    filterTypeField.Handler ??= filterExtensionField.Handler;
                }

                typeField.Description ??= extensionField.Description;
                typeField.RuntimeDefaultValue ??= extensionField.RuntimeDefaultValue;
            });
    }

    private static void MergeFilterFields(
        ITypeCompletionContext context,
        IList<InputFieldConfiguration> extensionFields,
        IList<InputFieldConfiguration> typeFields,
        Action<IList<InputFieldConfiguration>, InputFieldConfiguration, InputFieldConfiguration>
            action)
    {
        foreach (var extensionField in extensionFields)
        {
            InputFieldConfiguration? typeField;
            if (extensionField is FilterOperationFieldConfiguration operationFieldDefinition)
            {
                typeField = typeFields.OfType<FilterOperationFieldConfiguration>()
                    .FirstOrDefault(t => t.Id == operationFieldDefinition.Id);
            }
            else
            {
                typeField = typeFields.FirstOrDefault(
                    t => t.Name.EqualsOrdinal(extensionField.Name));
            }

            if (typeField is null)
            {
                typeFields.Add(extensionField);
            }
            else
            {
                TypeExtensionHelper.MergeDirectives(
                    context,
                    extensionField.Directives,
                    typeField.Directives);

                TypeExtensionHelper.MergeFeatures(extensionField, typeField);

                action(typeFields, extensionField, typeField);
            }
        }
    }

    private static void MergeSortEnumValues(
        ITypeCompletionContext context,
        IList<EnumValueConfiguration> extensionFields,
        IList<EnumValueConfiguration> typeFields,
        Action<IList<EnumValueConfiguration>, EnumValueConfiguration, EnumValueConfiguration>
            action)
    {
        foreach (var extensionField in extensionFields)
        {
            EnumValueConfiguration? typeField;
            if (extensionField is SortEnumValueConfiguration sortEnumValueDefinition)
            {
                typeField = typeFields.OfType<SortEnumValueConfiguration>()
                    .FirstOrDefault(t => t.Operation == sortEnumValueDefinition.Operation);
            }
            else
            {
                typeField = typeFields.FirstOrDefault(
                    t => t.Name.EqualsOrdinal(extensionField.Name));
            }

            if (typeField is null)
            {
                typeFields.Add(extensionField);
            }
            else
            {
                TypeExtensionHelper.MergeDirectives(
                    context,
                    extensionField.Directives,
                    typeField.Directives);

                TypeExtensionHelper.MergeFeatures(extensionField, typeField);

                action(typeFields, extensionField, typeField);
            }
        }
    }

    private static void MergeSortFields(
        ITypeCompletionContext context,
        IList<InputFieldConfiguration> extensionFields,
        IList<InputFieldConfiguration> typeFields,
        Action<IList<InputFieldConfiguration>, InputFieldConfiguration, InputFieldConfiguration>
            action)
    {
        foreach (var extensionField in extensionFields)
        {
            var typeField = typeFields.FirstOrDefault(
                t => t.Name.EqualsOrdinal(extensionField.Name));

            if (typeField is null)
            {
                typeFields.Add(extensionField);
            }
            else
            {
                TypeExtensionHelper.MergeDirectives(
                    context,
                    extensionField.Directives,
                    typeField.Directives);

                TypeExtensionHelper.MergeFeatures(extensionField, typeField);

                action(typeFields, extensionField, typeField);
            }
        }
    }
}
