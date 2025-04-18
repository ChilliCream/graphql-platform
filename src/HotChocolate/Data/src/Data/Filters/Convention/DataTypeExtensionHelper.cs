using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters;

internal static class DataTypeExtensionHelper
{
    public static void MergeFilterInputTypeDefinitions(
        ITypeCompletionContext context,
        FilterInputTypeDefinition extensionDefinition,
        FilterInputTypeDefinition typeDefinition)
    {
        TypeExtensionHelper.MergeContextData(
            extensionDefinition,
            typeDefinition);

        TypeExtensionHelper.MergeDirectives(
            context,
            extensionDefinition.Directives,
            typeDefinition.Directives);

        MergeFilterFieldDefinitions(
            context,
            extensionDefinition.Fields,
            typeDefinition.Fields);

        TypeExtensionHelper.MergeConfigurations(
            extensionDefinition.Tasks,
            typeDefinition.Tasks);
    }

    public static void MergeSortEnumTypeDefinitions(
        ITypeCompletionContext context,
        SortEnumTypeDefinition extensionDefinition,
        SortEnumTypeDefinition typeDefinition)
    {
        TypeExtensionHelper.MergeContextData(
            extensionDefinition,
            typeDefinition);

        TypeExtensionHelper.MergeDirectives(
            context,
            extensionDefinition.Directives,
            typeDefinition.Directives);

        MergeSortEnumValueDefinitions(
            context,
            extensionDefinition.Values,
            typeDefinition.Values);

        TypeExtensionHelper.MergeConfigurations(
            extensionDefinition.Tasks,
            typeDefinition.Tasks);
    }

    public static void MergeSortInputTypeDefinitions(
        ITypeCompletionContext context,
        SortInputTypeDefinition extensionDefinition,
        SortInputTypeDefinition typeDefinition)
    {
        TypeExtensionHelper.MergeContextData(
            extensionDefinition,
            typeDefinition);

        TypeExtensionHelper.MergeDirectives(
            context,
            extensionDefinition.Directives,
            typeDefinition.Directives);

        MergeSortInputFieldDefinitions(
            context,
            extensionDefinition.Fields,
            typeDefinition.Fields);

        TypeExtensionHelper.MergeConfigurations(
            extensionDefinition.Tasks,
            typeDefinition.Tasks);
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
                if (typeField is FilterFieldDefinition filterTypeField &&
                    extensionField is FilterFieldDefinition filterExtensionField)
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
                if (typeField is SortEnumValueConfiguration filterTypeField &&
                    extensionField is SortEnumValueConfiguration filterExtensionField)
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
                if (typeField is SortFieldDefinition filterTypeField &&
                    extensionField is SortFieldDefinition filterExtensionField)
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
            if (extensionField is FilterOperationFieldDefinition operationFieldDefinition)
            {
                typeField = typeFields.OfType<FilterOperationFieldDefinition>()
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

                TypeExtensionHelper.MergeContextData(extensionField, typeField);

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

                TypeExtensionHelper.MergeContextData(extensionField, typeField);

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

                TypeExtensionHelper.MergeContextData(extensionField, typeField);

                action(typeFields, extensionField, typeField);
            }
        }
    }
}
