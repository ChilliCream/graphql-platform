using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal sealed class FieldCollector
    {
        private readonly ISchema _schema;
        private readonly FragmentCollection _fragments;
        private readonly Cache _cache = new();

        public FieldCollector(ISchema schema, FragmentCollection fragments)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public SelectionVariants CollectFields(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path)
        {
            if (!_cache.TryGetValue(type, out SelectionCache? selectionCache))
            {
                selectionCache = new SelectionCache();
                _cache.Add(type, selectionCache);
            }

            if (!selectionCache.TryGetValue(selectionSet, out SelectionVariants? selectionVariants))
            {
                Selection returnType = CollectFieldsInternal(type, selectionSet, path);

                if (type.IsAbstractType())
                {
                    var list = new List<Selection>();
                    var singleModelShape = true;

                    foreach (ObjectType objectType in _schema.GetPossibleTypes(type))
                    {
                        Selection objectSelection =
                            CollectFieldsInternal(objectType, selectionSet, path);
                        list.Add(objectSelection);

                        if (!FieldSelectionsAreEqual(
                            returnType.Fields,
                            objectSelection.Fields))
                        {
                            singleModelShape = false;
                        }
                    }

                    if (!singleModelShape)
                    {
                        selectionVariants = new SelectionVariants(returnType, list);
                    }
                }

                if (selectionVariants is null)
                {
                    selectionVariants = new SelectionVariants(returnType);
                }

                selectionCache.Add(selectionSet, selectionVariants);
            }

            return selectionVariants;
        }

        private Selection CollectFieldsInternal(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            var fields = new OrderedDictionary<string, FieldSelection>();
            var fragments = new List<IFragmentNode>();

            CollectFields(type, selectionSet, path, fields, fragments);

            return new Selection(
                type,
                selectionSet,
                fields.Values.ToList(),
                fragments);
        }

        private void CollectFields(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path,
            IDictionary<string, FieldSelection> fields,
            ICollection<IFragmentNode> fragments)
        {
            foreach (ISelectionNode selection in selectionSet.Selections)
            {
                ResolveFields(
                    type,
                    selection,
                    path,
                    fields,
                    fragments);
            }
        }

        private void ResolveFields(
            INamedOutputType type,
            ISelectionNode selection,
            Path path,
            IDictionary<string, FieldSelection> fields,
            ICollection<IFragmentNode> fragments)
        {
            if (selection is FieldNode fs && type is IComplexOutputType ct)
            {
                ResolveFieldSelection(
                    ct,
                    fs,
                    path,
                    fields);
            }
            else if (selection is FragmentSpreadNode fragSpread)
            {
                ResolveFragmentSpread(
                    type,
                    fragSpread,
                    path,
                    fields,
                    fragments);
            }
            else if (selection is InlineFragmentNode inlineFrag)
            {
                ResolveInlineFragment(
                    type,
                    inlineFrag,
                    path,
                    fields,
                    fragments);
            }
        }

        internal static void ResolveFieldSelection(
            IComplexOutputType type,
            FieldNode fieldSelection,
            Path path,
            IDictionary<string, FieldSelection> fields)
        {
            NameString fieldName = fieldSelection.Name.Value;
            NameString responseName = fieldSelection.Alias == null
                ? fieldSelection.Name.Value
                : fieldSelection.Alias.Value;

            if (type.Fields.TryGetField(fieldName, out IOutputField? field))
            {
                if (!fields.TryGetValue(responseName, out FieldSelection? f))
                {
                    f = new FieldSelection(field, fieldSelection, path);
                    fields.Add(responseName, f);
                }
            }
            else
            {
                // TODO : resources
                throw new InvalidOperationException(
                    $"Field `{fieldName}` does not exist in type `{type.Name}`.");
            }
        }

        private void ResolveFragmentSpread(
            INamedOutputType type,
            FragmentSpreadNode fragmentSpread,
            Path path,
            IDictionary<string, FieldSelection> fields,
            ICollection<IFragmentNode> fragments)
        {
            Fragment fragment = _fragments.GetFragment(fragmentSpread.Name.Value);

            if (TypeHelpers.DoesTypeApply(fragment.TypeCondition, type))
            {
                var fragmentNode = new FragmentNode(fragment);
                fragments.Add(fragmentNode);

                CollectFields(
                    type,
                    fragment.SelectionSet,
                    path,
                    fields,
                    fragmentNode.Children);
            }
        }

        private void ResolveInlineFragment(
            INamedOutputType type,
            InlineFragmentNode inlineFragment,
            Path path,
            IDictionary<string, FieldSelection> fields,
            ICollection<IFragmentNode> fragments)
        {
            Fragment fragment = _fragments.GetFragment(type, inlineFragment);

            if (TypeHelpers.DoesTypeApply(fragment.TypeCondition, type))
            {
                var fragmentNode = new FragmentNode(new Fragment(
                    fragment.TypeCondition.Name,
                    fragment.Kind,
                    fragment.TypeCondition,
                    fragment.SelectionSet));
                fragments.Add(fragmentNode);

                CollectFields(
                    type,
                    fragment.SelectionSet,
                    path,
                    fields,
                    fragmentNode.Children);
            }
        }

        private static bool FieldSelectionsAreEqual(
            IReadOnlyList<FieldSelection> a,
            IReadOnlyList<FieldSelection> b)
        {
            if (a.Count == b.Count)
            {
                for (var i = 0; i < a.Count; i++)
                {
                    if (!ReferenceEquals(a[i].FieldSyntax, b[i].FieldSyntax))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private class Cache : Dictionary<INamedOutputType, SelectionCache>
        { }

        private class SelectionCache : Dictionary<SelectionSetNode, SelectionVariants>
        { }
    }
}
