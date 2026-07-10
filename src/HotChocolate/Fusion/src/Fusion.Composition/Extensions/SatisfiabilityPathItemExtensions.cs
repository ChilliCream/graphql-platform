using HotChocolate.Fusion.Collections;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Extensions;

internal static class SatisfiabilityPathItemExtensions
{
    extension(SatisfiabilityPathItem item)
    {
        /// <summary>
        /// Determines if the <see cref="SatisfiabilityPathItem"/> provides the given field on the given
        /// type and schema.
        /// </summary>
        public bool Provides(
            MutableOutputFieldDefinition field,
            MutableObjectTypeDefinition type,
            string schemaName,
            MutableSchemaDefinition schema)
            => item.TryGetProvidedSelectionSet(field, type, schemaName, schema, out _);

        /// <summary>
        /// Determines whether the path item provides the given field via an event stream message.
        /// Unlike <see cref="Provides"/>, an <c>@provides</c> selection never counts here, because
        /// <c>@provides</c> is an optimization that must not make a query path satisfiable.
        /// </summary>
        public bool ProvidesViaEventStream(
            MutableOutputFieldDefinition field,
            MutableObjectTypeDefinition type,
            string schemaName,
            MutableSchemaDefinition schema)
            => item.ProvidedByEventStream
                && item.TryGetProvidedSelectionSet(field, type, schemaName, schema, out _);

        public bool TryGetProvidedSelectionSet(
            MutableOutputFieldDefinition field,
            MutableObjectTypeDefinition type,
            string schemaName,
            MutableSchemaDefinition schema,
            out SelectionSetNode? providedSelectionSet)
        {
            providedSelectionSet = null;

            if (item.SchemaName != schemaName)
            {
                return false;
            }

            if (item.ProvidedSelectionSet is { } itemProvidedSelectionSet)
            {
                return SelectionSetProvider.TryGetSelectionSet(
                    schema,
                    itemProvidedSelectionSet,
                    item.FieldType,
                    field,
                    type,
                    out providedSelectionSet);
            }

            var selectionSetText = item.Field.GetFusionFieldProvides(item.SchemaName);

            if (selectionSetText is null)
            {
                return false;
            }

            var selectionSet = ParseSelectionSet($"{{ {selectionSetText} }}");

            return SelectionSetProvider.TryGetSelectionSet(
                schema,
                selectionSet,
                item.FieldType,
                field,
                type,
                out providedSelectionSet);
        }
    }

    private static class SelectionSetProvider
    {
        public static bool TryGetSelectionSet(
            ISchemaDefinition schema,
            SelectionSetNode selectionSet,
            ITypeDefinition type,
            IOutputFieldDefinition field,
            ITypeDefinition declaringType,
            out SelectionSetNode? providedSelectionSet)
        {
            List<ISelectionNode>? selections = null;
            var isSelected = TryCollectSelectionSet(
                schema,
                selectionSet,
                type,
                field,
                declaringType,
                ref selections);

            providedSelectionSet = selections is { Count: > 0 }
                ? new SelectionSetNode(selections)
                : null;

            return isSelected;
        }

        private static bool TryCollectSelectionSet(
            ISchemaDefinition schema,
            SelectionSetNode selectionSet,
            ITypeDefinition type,
            IOutputFieldDefinition field,
            ITypeDefinition declaringType,
            ref List<ISelectionNode>? selections)
        {
            var isSelected = false;

            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode fieldNode:
                        if (type is not IComplexTypeDefinition complexType
                            || !complexType.Fields.TryGetField(fieldNode.Name.Value, out var selectedField)
                            || selectedField.Name != field.Name
                            || !type.IsAssignableFrom(declaringType))
                        {
                            break;
                        }

                        isSelected = true;

                        if (fieldNode.SelectionSet is { } childSelectionSet)
                        {
                            selections ??= [];
                            selections.AddRange(childSelectionSet.Selections);
                        }

                        break;

                    case InlineFragmentNode { TypeCondition: null } inlineFragment:
                        isSelected |= TryCollectSelectionSet(
                            schema,
                            inlineFragment.SelectionSet,
                            type,
                            field,
                            declaringType,
                            ref selections);

                        break;

                    case InlineFragmentNode inlineFragment:
                        if (inlineFragment.TypeCondition is null
                            || !schema.Types.TryGetType(
                                inlineFragment.TypeCondition.Name.Value,
                                out var typeCondition)
                            || !TypesOverlap(schema, type, typeCondition))
                        {
                            break;
                        }

                        isSelected |= TryCollectSelectionSet(
                            schema,
                            inlineFragment.SelectionSet,
                            typeCondition,
                            field,
                            declaringType,
                            ref selections);

                        break;
                }
            }

            return isSelected;
        }

        private static bool TypesOverlap(
            ISchemaDefinition schema,
            ITypeDefinition left,
            ITypeDefinition right)
        {
            foreach (var leftPossibleType in schema.GetPossibleTypes(left))
            {
                foreach (var rightPossibleType in schema.GetPossibleTypes(right))
                {
                    if (leftPossibleType == rightPossibleType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
