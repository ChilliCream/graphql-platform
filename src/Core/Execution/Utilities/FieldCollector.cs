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
                string fieldName = fs.Name.Value;
                if (type.Fields.TryGetField(fieldName, out ObjectField field))
                {
                    string name = fs.Alias == null ? fs.Name.Value : fs.Alias.Value;
                    fields[name] = new FieldSelection(fs, field, name);
                }
                else
                {
                    reportError(new FieldError(
                        "Could not resolve the specified field.",
                        fs));
                }
            }
            else if (selection is FragmentSpreadNode fragmentSpread)
            {
                Fragment fragment = _fragments.GetFragments(fragmentSpread.Name.Value)
                    .FirstOrDefault(t => DoesFragmentTypeApply(type, t.TypeCondition));
                if (fragment != null)
                {
                    CollectFields(type, fragment.SelectionSet, reportError, fields);
                }
            }
            else if (selection is InlineFragmentNode inlineFragment)
            {
                Fragment fragment = _fragments.GetFragment(type, inlineFragment);
                if (DoesFragmentTypeApply(type, fragment.TypeCondition))
                {
                    CollectFields(type, fragment.SelectionSet, reportError, fields);
                }
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

        private bool DoesFragmentTypeApply(ObjectType objectType, IType type)
        {
            if (type is ObjectType ot)
            {
                return ot == objectType;
            }
            else if (type is InterfaceType it)
            {
                return objectType.Interfaces.ContainsKey(it.Name);
            }
            else if (type is UnionType ut)
            {
                return ut.Types.ContainsKey(objectType.Name);
            }
            return false;
        }
    }
}
