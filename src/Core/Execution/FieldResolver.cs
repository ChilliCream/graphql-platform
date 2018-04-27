using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class FieldResolver
    {
        public List<FieldSelection> CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            VariableCollection variables,
            FragmentCollection fragments,
            Action<QueryError> reportError)
        {
            List<FieldSelection> fields = new List<FieldSelection>();
            CollectFields(type, selectionSet, variables,
                fragments, reportError, fields);
            return fields;
        }

        private void CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            VariableCollection variables,
            FragmentCollection fragments,
            Action<QueryError> reportError,
            List<FieldSelection> fields)
        {
            foreach (ISelectionNode selection in selectionSet.Selections)
            {
                if (ShouldBeIncluded(selection, variables))
                {
                    ResolveFields(type, selection, variables,
                        fragments, reportError, fields);
                }
            }
        }

        private void ResolveFields(
            ObjectType type,
            ISelectionNode selection,
            VariableCollection variables,
            FragmentCollection fragments,
            Action<QueryError> reportError,
            List<FieldSelection> fields)
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
                fields.Add(new FieldSelection(fs, field, name));
            }

            if (selection is FragmentSpreadNode fragmentSpread)
            {
                Fragment fragment = fragments.GetFragments(fragmentSpread.Name.Value)
                    .FirstOrDefault(t => DoesFragmentTypeApply(type, t.TypeCondition));
                if (fragment == null)
                {
                    return;
                }

                CollectFields(type, fragment.SelectionSet,
                    variables, fragments, reportError, fields);
            }

            if (selection is InlineFragmentNode inlineFragment)
            {
                Fragment fragment = fragments.GetFragment(inlineFragment);
                if (DoesFragmentTypeApply(type, fragment.Type))
                {
                    CollectFields(type, fragment.SelectionSet,
                        variables, fragments, reportError, fields);
                }
            }
        }

        private bool ShouldBeIncluded(ISelectionNode selection, VariableCollection variables)
        {
            if (selection.Directives.Skip(variables))
            {
                return false;
            }
            return selection.Directives.Include(variables);
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
