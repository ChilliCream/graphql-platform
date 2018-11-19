using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class FieldCollector
    {
        private readonly VariableCollection _variables;
        private readonly FragmentCollection _fragments;

        public FieldCollector(
            VariableCollection variables,
            FragmentCollection fragments)
        {
            _variables = variables
                ?? throw new ArgumentNullException(nameof(variables));
            _fragments = fragments
                ?? throw new ArgumentNullException(nameof(fragments));
        }

        public IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            Action<QueryError> reportError)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            if (reportError == null)
            {
                throw new ArgumentNullException(nameof(reportError));
            }

            var fields = new Dictionary<string, FieldSelection>();
            CollectFields(type, selectionSet, reportError, fields);
            return fields.Values;
        }

        private void CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            Action<QueryError> reportError,
            Dictionary<string, FieldSelection> fields)
        {
            foreach (ISelectionNode selection in selectionSet.Selections)
            {
                if (ShouldBeIncluded(selection))
                {
                    ResolveFields(type, selection, reportError, fields);
                }
            }
        }

        private void ResolveFields(
            ObjectType type,
            ISelectionNode selection,
            Action<QueryError> reportError,
            Dictionary<string, FieldSelection> fields)
        {
            if (selection is FieldNode fs)
            {
                ResolveFieldSelection(type, fs, reportError, fields);
            }
            else if (selection is FragmentSpreadNode fragSpread)
            {
                ResolveFragmentSpread(type, fragSpread, reportError, fields);
            }
            else if (selection is InlineFragmentNode inlineFrag)
            {
                ResolveInlineFragment(type, inlineFrag, reportError, fields);
            }
        }

        private void ResolveFieldSelection(
            ObjectType type,
            FieldNode fieldSelection,
            Action<QueryError> reportError,
            Dictionary<string, FieldSelection> fields)
        {
            NameString fieldName = fieldSelection.Name.Value;
            if (type.Fields.TryGetField(fieldName, out ObjectField field))
            {
                string name = fieldSelection.Alias == null
                    ? fieldSelection.Name.Value
                    : fieldSelection.Alias.Value;
                fields[name] = new FieldSelection(fieldSelection, field, name);
            }
            else
            {
                reportError(QueryError.CreateFieldError(
                    "Could not resolve the specified field.",
                    fieldSelection));
            }
        }

        private void ResolveFragmentSpread(
            ObjectType type,
            FragmentSpreadNode fragmentSpread,
            Action<QueryError> reportError,
            Dictionary<string, FieldSelection> fields)
        {
            Fragment fragment = _fragments.GetFragment(
                fragmentSpread.Name.Value);

            if (fragment != null && DoesTypeApply(fragment.TypeCondition, type))
            {
                CollectFields(type, fragment.SelectionSet, reportError, fields);
            }
        }

        private void ResolveInlineFragment(
            ObjectType type,
            InlineFragmentNode inlineFragment,
            Action<QueryError> reportError,
            Dictionary<string, FieldSelection> fields)
        {
            Fragment fragment = _fragments.GetFragment(type, inlineFragment);
            if (DoesTypeApply(fragment.TypeCondition, type))
            {
                CollectFields(type, fragment.SelectionSet, reportError, fields);
            }
        }


        private bool ShouldBeIncluded(ISelectionNode selection)
        {
            if (selection.Directives.Skip(_variables))
            {
                return false;
            }
            return selection.Directives.Include(_variables);
        }

        private bool DoesTypeApply(IType typeCondition, ObjectType current)
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
