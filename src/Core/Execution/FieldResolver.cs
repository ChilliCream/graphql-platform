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
        private readonly Schema _schema;
        private readonly VariableCollection _variables;
        private readonly FragmentCollection _fragments;

        public FieldResolver(
            Schema schema,
            VariableCollection variables,
            FragmentCollection fragments)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            if (fragments == null)
            {
                throw new ArgumentNullException(nameof(fragments));
            }

            _schema = schema;
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
                if (fieldName.StartsWith("__"))
                {
                    ResolveIntrospectionField(type, fs, reportError, fields);
                }
                else if (type.Fields.TryGetValue(fieldName, out Field field))
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
                Fragment fragment = _fragments.GetFragment(inlineFragment);
                if (DoesFragmentTypeApply(type, fragment.TypeCondition))
                {
                    CollectFields(type, fragment.SelectionSet, reportError, fields);
                }
            }
        }

        private void ResolveIntrospectionField(
           ObjectType type,
           FieldNode fieldNode,
           Action<QueryError> reportError,
           Dictionary<string, FieldSelection> fields)
        {
            string name = fieldNode.Alias == null
                ? fieldNode.Name.Value
                : fieldNode.Alias.Value;

            if (_schema.TypeNameField.Name
                .Equals(fieldNode.Name.Value, StringComparison.Ordinal))
            {
                fields[name] = new FieldSelection(
                    fieldNode, _schema.TypeNameField, name);
            }
            else if (_schema.QueryType == type
                && _schema.TypeField.Name
                    .Equals(fieldNode.Name.Value, StringComparison.Ordinal))
            {
                fields[name] = new FieldSelection(
                    fieldNode, _schema.TypeField, name);
            }
            else if (_schema.QueryType == type
                && _schema.SchemaField.Name
                    .Equals(fieldNode.Name.Value, StringComparison.Ordinal))
            {
                fields[name] = new FieldSelection(
                    fieldNode, _schema.SchemaField, name);
            }
            else
            {
                reportError(new FieldError(
                    "The specified introspection field does not exist.",
                    fieldNode));
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
