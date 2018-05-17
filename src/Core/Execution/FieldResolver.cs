using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    // TODO : Rename this class or Resolvers.FieldResolver
    internal class FieldResolver
    {
        private readonly VariableCollection _variables;
        private readonly FragmentCollection _fragments;

        public FieldResolver(
            VariableCollection variables,
            FragmentCollection fragments)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            if (fragments == null)
            {
                throw new ArgumentNullException(nameof(fragments));
            }

            _variables = variables;
            _fragments = fragments;
        }

        public IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            Action<QueryError> reportError)
        {
            Dictionary<string, FieldSelection> fields =
                new Dictionary<string, FieldSelection>();
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
                if (!type.Fields.TryGetValue(fieldName, out Field field))
                {
                    reportError(new FieldError(
                        "Could not resolve the specified field.",
                        fs));
                    return;
                }

                string name = fs.Alias == null ? fs.Name.Value : fs.Alias.Value;
                fields[name] = new FieldSelection(fs, field, name);
            }

            if (selection is FragmentSpreadNode fragmentSpread)
            {
                Fragment fragment = _fragments.GetFragments(fragmentSpread.Name.Value)
                    .FirstOrDefault(t => DoesFragmentTypeApply(type, t.TypeCondition));
                if (fragment == null)
                {
                    return;
                }

                CollectFields(type, fragment.SelectionSet, reportError, fields);
            }

            if (selection is InlineFragmentNode inlineFragment)
            {
                Fragment fragment = _fragments.GetFragment(inlineFragment);
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
