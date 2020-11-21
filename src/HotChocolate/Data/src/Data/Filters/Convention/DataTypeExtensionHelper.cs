using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
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
                extensionDefinition.Configurations,
                typeDefinition.Configurations);
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
                extensionDefinition.Configurations,
                typeDefinition.Configurations);
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
                extensionDefinition.Configurations,
                typeDefinition.Configurations);
        }

        private static void MergeFilterFieldDefinitions(
            ITypeCompletionContext context,
            IList<InputFieldDefinition> extensionFields,
            IList<InputFieldDefinition> typeFields)
        {
            MergeFilterFields(
                context,
                extensionFields,
                typeFields,
                (fields, extensionField, typeField) =>
                {
                    if (typeField is FilterFieldDefinition filterTypeField &&
                        extensionField is FilterFieldDefinition filterExtensionField)
                    {
                        filterTypeField.Handler ??= filterExtensionField.Handler;
                    }

                    typeField.Description ??= extensionField.Description;
                    typeField.NativeDefaultValue ??= extensionField.NativeDefaultValue;
                });
        }

        private static void MergeSortEnumValueDefinitions(
            ITypeCompletionContext context,
            IList<EnumValueDefinition> extensionFields,
            IList<EnumValueDefinition> typeFields)
        {
            MergeSortEnumValues(
                context,
                extensionFields,
                typeFields,
                (fields, extensionField, typeField) =>
                {
                    if (typeField is SortEnumValueDefinition filterTypeField &&
                        extensionField is SortEnumValueDefinition filterExtensionField)
                    {
                        filterTypeField.Handler ??= filterExtensionField.Handler;
                    }

                    typeField.Description ??= extensionField.Description;
                });
        }

        private static void MergeSortInputFieldDefinitions(
            ITypeCompletionContext context,
            IList<InputFieldDefinition> extensionFields,
            IList<InputFieldDefinition> typeFields)
        {
            MergeSortFields(
                context,
                extensionFields,
                typeFields,
                (fields, extensionField, typeField) =>
                {
                    if (typeField is SortFieldDefinition filterTypeField &&
                        extensionField is SortFieldDefinition filterExtensionField)
                    {
                        filterTypeField.Handler ??= filterExtensionField.Handler;
                    }

                    typeField.Description ??= extensionField.Description;
                    typeField.NativeDefaultValue ??= extensionField.NativeDefaultValue;
                });
        }

        private static void MergeFilterFields(
            ITypeCompletionContext context,
            IList<InputFieldDefinition> extensionFields,
            IList<InputFieldDefinition> typeFields,
            Action<IList<InputFieldDefinition>, InputFieldDefinition, InputFieldDefinition>
                action)
        {
            foreach (var extensionField in extensionFields)
            {
                InputFieldDefinition? typeField;
                if (extensionField is FilterOperationFieldDefinition operationFieldDefinition)
                {
                    typeField = typeFields.OfType<FilterOperationFieldDefinition>()
                        .FirstOrDefault(t => t.Id == operationFieldDefinition.Id);
                }
                else
                {
                    typeField = typeFields.FirstOrDefault(
                        t => t.Name.Equals(extensionField.Name));
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
            IList<EnumValueDefinition> extensionFields,
            IList<EnumValueDefinition> typeFields,
            Action<IList<EnumValueDefinition>, EnumValueDefinition, EnumValueDefinition>
                action)
        {
            foreach (var extensionField in extensionFields)
            {
                EnumValueDefinition? typeField;
                if (extensionField is SortEnumValueDefinition sortEnumValueDefinition)
                {
                    typeField = typeFields.OfType<SortEnumValueDefinition>()
                        .FirstOrDefault(t => t.Operation == sortEnumValueDefinition.Operation);
                }
                else
                {
                    typeField = typeFields.FirstOrDefault(
                        t => t.Name.Equals(extensionField.Name));
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
            IList<InputFieldDefinition> extensionFields,
            IList<InputFieldDefinition> typeFields,
            Action<IList<InputFieldDefinition>, InputFieldDefinition, InputFieldDefinition>
                action)
        {
            foreach (var extensionField in extensionFields)
            {
                InputFieldDefinition? typeField = typeFields.FirstOrDefault(
                    t => t.Name.Equals(extensionField.Name));

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
}
