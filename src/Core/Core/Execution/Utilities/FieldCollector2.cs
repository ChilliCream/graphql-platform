using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed partial class FieldCollector2
    {
        private readonly FragmentCollection _fragments;
        private readonly Func<ObjectField, FieldNode, FieldDelegate> _createMiddleware;

        public FieldCollector2(
            FragmentCollection fragments,
            Func<FieldSelection, FieldDelegate> createMiddleware)
        {
            _fragments = fragments
                ?? throw new ArgumentNullException(nameof(fragments));
            _createMiddleware = createMiddleware
                ?? throw new ArgumentNullException(nameof(createMiddleware));
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

            var fields = new OrderedDictionary<string, FieldSelection>();
            CollectFields(type, selectionSet, reportError, fields);
            return fields.Values;
        }

        private void CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            Action<IError> reportError,
            IDictionary<string, FieldInfo> fields)
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
            IDictionary<string, FieldInfo> fields)
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
            FieldVisibility fieldVisibility,
            Action<IError> reportError,
            IDictionary<string, FieldInfo> fields)
        {
            NameString fieldName = fieldSelection.Name.Value;
            if (type.Fields.TryGetField(fieldName, out ObjectField field))
            {
                string name = fieldSelection.Alias == null
                    ? fieldSelection.Name.Value
                    : fieldSelection.Alias.Value;

                if (fields.TryGetValue(name, out FieldInfo fieldInfo))
                {
                    if (fieldInfo.Nodes == null)
                    {
                        fieldInfo.Nodes = new List<FieldNode>();
                    }

                    fieldInfo.Nodes.Add(fieldSelection);

                    if (fieldVisibility != null)
                    {
                        if (fieldInfo.Visibilities == null)
                        {
                            fieldInfo.Visibilities =
                                new List<FieldVisibility>();
                        }
                        fieldInfo.Visibilities.Add(fieldVisibility);
                    }
                }
                else
                {
                    fieldInfo = new FieldInfo
                    {
                        Field = field,
                        ResponseName = fieldName,
                        Selection = fieldSelection,
                        Middleware = _createMiddleware(field, fieldSelection)
                    };

                    if (fieldVisibility != null)
                    {
                        fieldInfo.Visibilities = new List<FieldVisibility>();
                        fieldInfo.Visibilities.Add(fieldVisibility);
                    }

                    fields.Add(name, fieldInfo);
                }
            }
            else
            {
                reportError(ErrorBuilder.New()
                    .SetMessage("Could not resolve the specified field.")
                    .AddLocation(fieldSelection)
                    .Build());
            }
        }

        private void ResolveFragmentSpread(
            ObjectType type,
            FragmentSpreadNode fragmentSpread,
            Action<IError> reportError,
            IDictionary<string, FieldSelection> fields)
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
            Action<IError> reportError,
            IDictionary<string, FieldSelection> fields)
        {
            Fragment fragment = _fragments.GetFragment(type, inlineFragment);
            if (DoesTypeApply(fragment.TypeCondition, type))
            {
                CollectFields(type, fragment.SelectionSet, reportError, fields);
            }
        }

        private bool ShouldBeIncluded(Language.IHasDirectives selection)
        {
            if (selection.Directives.Skip(_variables))
            {
                return false;
            }
            return selection.Directives.Include(_variables);
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
