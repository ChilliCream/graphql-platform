using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Utilities
{
    internal sealed class FieldCollector
    {
        private const string _argumentProperty = "argument";
        private readonly FragmentCollection _fragments;

        public FieldCollector(FragmentCollection fragments)
        {
            _fragments = fragments
                ?? throw new ArgumentNullException(nameof(fragments));
        }

        public FieldCollectionResult CollectFields(
            ObjectType type,
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
            var root = new FragmentNode();
            CollectFields(type, selectionSet, path, fields, root);

            return new FieldCollectionResult(
                type,
                fields.Values.ToList(),
                root.Children);
        }

        private void CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            Path path,
            IDictionary<string, FieldSelection> fields,
            FragmentNode parent)
        {
            foreach (ISelectionNode selection in selectionSet.Selections)
            {
                ResolveFields(
                    type,
                    selection,
                    path,
                    fields,
                    parent);
            }
        }

        private void ResolveFields(
            ObjectType type,
            ISelectionNode selection,
            Path path,
            IDictionary<string, FieldSelection> fields,
            FragmentNode parent)
        {
            if (selection is FieldNode fs)
            {
                ResolveFieldSelection(
                    type,
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
                    parent);
            }
            else if (selection is InlineFragmentNode inlineFrag)
            {
                ResolveInlineFragment(
                    type,
                    inlineFrag,
                    path,
                    fields,
                    parent);
            }
        }

        private void ResolveFieldSelection(
            ObjectType type,
            FieldNode fieldSelection,
            Path path,
            IDictionary<string, FieldSelection> fields)
        {
            NameString fieldName = fieldSelection.Name.Value;
            NameString responseName = fieldSelection.Alias == null
                ? fieldSelection.Name.Value
                : fieldSelection.Alias.Value;

            if (type.Fields.TryGetField(fieldName, out ObjectField field))
            {
                if (!fields.TryGetValue(responseName, out FieldSelection f))
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
            ObjectType type,
            FragmentSpreadNode fragmentSpread,
            Path path,
            IDictionary<string, FieldSelection> fields,
            FragmentNode parent)
        {
            Fragment fragment = _fragments.GetFragment(
                fragmentSpread.Name.Value);
            FragmentNode fragmentNode = parent.AddChild(fragment);

            if (fragment != null && DoesTypeApply(fragment.TypeCondition, type))
            {
                CollectFields(
                    type,
                    fragment.SelectionSet,
                    path,
                    fields,
                    fragmentNode);
            }
        }

        private void ResolveInlineFragment(
            ObjectType type,
            InlineFragmentNode inlineFragment,
            Path path,
            IDictionary<string, FieldSelection> fields,
            FragmentNode parent)
        {
            Fragment fragment = _fragments.GetFragment(type, inlineFragment);

            if (DoesTypeApply(fragment.TypeCondition, type))
            {
                CollectFields(
                    type,
                    fragment.SelectionSet,
                    path,
                    fields,
                    parent);
            }
        }

        private static bool DoesTypeApply(
            IType typeCondition,
            ObjectType current)
        {
            if (typeCondition is ObjectType ot)
            {
                return ot == current;
            }
            else if (typeCondition is InterfaceType it)
            {
                return current.Interfaces.ContainsKey(it.Name);
            }
            else if (typeCondition is UnionType ut)
            {
                return ut.Types.ContainsKey(current.Name);
            }
            return false;
        }
    }


}
