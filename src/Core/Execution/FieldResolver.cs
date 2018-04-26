using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class FieldResolver
    {
        public List<FieldSelection> CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            VariableCollection variables,
            Action<QueryError> reportError)
        {
            List<FieldSelection> fields = new List<FieldSelection>();

            foreach (ISelectionNode selection in selectionSet.Selections)
            {
                if (ShouldBeIncluded(selection, variables)
                    && TryResolveSelection(
                        type, variables, selection,
                        reportError, out FieldSelection fieldSelection))
                {
                    fields.Add(fieldSelection);
                }
            }

            return fields;
        }

        private bool TryResolveSelection(
            ObjectType type,
            VariableCollection variables,
            ISelectionNode selection,
            Action<QueryError> reportError,
            out FieldSelection fieldSelection)
        {
            if (selection is FieldNode fs)
            {
                string fieldName = fs.Name.Value;
                if (!type.Fields.TryGetValue(fieldName, out Field field))
                {
                    reportError(new FieldError(
                        "Could not resolve the specified field.",
                        fs));
                    fieldSelection = null;
                    return false;
                }

                string name = fs.Alias == null ? fs.Name.Value : fs.Alias.Value;
                fieldSelection = new FieldSelection(fs, field, name);
                return false;
            }

            if (selection is FragmentSpreadNode fragmentSpread)
            {

            }

            if (selection is InlineFragmentNode inlineFragment)
            {

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



    }
}
