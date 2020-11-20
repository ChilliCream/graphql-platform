using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    internal static class FilterTypeExtensionHelper
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
    }
}
