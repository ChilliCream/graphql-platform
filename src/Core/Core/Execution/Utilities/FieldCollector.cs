using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class FieldCollector
    {
        private const string _argumentProperty = "argument";

        private readonly FragmentCollection _fragments;
        private readonly Func<ObjectField, FieldNode, FieldDelegate> _factory;

        public FieldCollector(
            FragmentCollection fragments,
            Func<ObjectField, FieldNode, FieldDelegate> middlewareFactory)
        {
            _fragments = fragments
                ?? throw new ArgumentNullException(nameof(fragments));
            _factory = middlewareFactory
                ?? throw new ArgumentNullException(nameof(middlewareFactory));
        }

        public IReadOnlyList<FieldSelection> CollectFields(
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

            var fields = new OrderedDictionary<string, FieldInfo>();
            CollectFields(type, selectionSet, path, null, fields);

            int i = 0;
            var fieldSelections = new FieldSelection[fields.Count];
            foreach (FieldInfo field in fields.Values)
            {
                field.Middleware = _factory(field.Field, field.Selection);
                fieldSelections[i++] = new FieldSelection(field);
            }
            return fieldSelections;
        }

        private void CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            Path path,
            FieldVisibility fieldVisibility,
            IDictionary<string, FieldInfo> fields)
        {
            foreach (ISelectionNode selection in selectionSet.Selections)
            {
                ResolveFields(
                    type,
                    selection,
                    path,
                    ExtractVisibility(selection, fieldVisibility),
                    fields);
            }
        }

        private void ResolveFields(
            ObjectType type,
            ISelectionNode selection,
            Path path,
            FieldVisibility fieldVisibility,
            IDictionary<string, FieldInfo> fields)
        {
            if (selection is FieldNode fs)
            {
                ResolveFieldSelection(
                    type,
                    fs,
                    path,
                    fieldVisibility,
                    fields);
            }
            else if (selection is FragmentSpreadNode fragSpread)
            {
                ResolveFragmentSpread(
                    type,
                    fragSpread,
                    path,
                    fieldVisibility,
                    fields);
            }
            else if (selection is InlineFragmentNode inlineFrag)
            {
                ResolveInlineFragment(
                    type,
                    inlineFrag,
                    path,
                    fieldVisibility,
                    fields);
            }
        }

        private void ResolveFieldSelection(
            ObjectType type,
            FieldNode fieldSelection,
            Path path,
            FieldVisibility fieldVisibility,
            IDictionary<string, FieldInfo> fields)
        {
            NameString fieldName = fieldSelection.Name.Value;
            if (type.Fields.TryGetField(fieldName, out ObjectField field))
            {
                NameString responseName = fieldSelection.Alias == null
                    ? fieldSelection.Name.Value
                    : fieldSelection.Alias.Value;

                if (fields.TryGetValue(responseName, out FieldInfo fieldInfo))
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
                        ResponseName = responseName,
                        Selection = fieldSelection,
                        Path = path
                    };

                    if (fieldVisibility != null)
                    {
                        fieldInfo.Visibilities = new List<FieldVisibility>();
                        fieldInfo.Visibilities.Add(fieldVisibility);
                    }

                    CoerceArgumentValues(fieldInfo);

                    fields.Add(responseName, fieldInfo);
                }
            }
            else
            {
                throw new QueryException(ErrorBuilder.New()
                    .SetMessage(CoreResources.FieldCollector_FieldNotFound)
                    .SetPath(path)
                    .AddLocation(fieldSelection)
                    .Build());
            }
        }

        private void ResolveFragmentSpread(
            ObjectType type,
            FragmentSpreadNode fragmentSpread,
            Path path,
            FieldVisibility fieldVisibility,
            IDictionary<string, FieldInfo> fields)
        {
            Fragment fragment = _fragments.GetFragment(
                fragmentSpread.Name.Value);

            if (fragment != null && DoesTypeApply(fragment.TypeCondition, type))
            {
                CollectFields(
                    type,
                    fragment.SelectionSet,
                    path,
                    fieldVisibility,
                    fields);
            }
        }

        private void ResolveInlineFragment(
            ObjectType type,
            InlineFragmentNode inlineFragment,
            Path path,
            FieldVisibility fieldVisibility,
            IDictionary<string, FieldInfo> fields)
        {
            Fragment fragment = _fragments.GetFragment(type, inlineFragment);
            if (DoesTypeApply(fragment.TypeCondition, type))
            {
                CollectFields(
                    type,
                    fragment.SelectionSet,
                    path,
                    fieldVisibility,
                    fields);
            }
        }

        private FieldVisibility ExtractVisibility(
            Language.IHasDirectives selection,
            FieldVisibility fieldVisibility)
        {
            IValueNode skip = selection.Directives.SkipValue();
            IValueNode include = selection.Directives.IncludeValue();

            if (skip == null && include == null)
            {
                return fieldVisibility;
            }

            return new FieldVisibility(skip, include, fieldVisibility);
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

        private static void CoerceArgumentValues(FieldInfo fieldInfo)
        {
            var argumentValues = fieldInfo.Selection.Arguments
                .Where(t => t.Value != null)
                .ToDictionary(t => t.Name.Value, t => t.Value);

            foreach (Argument argument in fieldInfo.Field.Arguments)
            {
                try
                {
                    CoerceArgumentValue(
                        fieldInfo,
                        argument,
                        argumentValues);
                }
                catch (ScalarSerializationException ex)
                {
                    fieldInfo.Arguments[argument.Name] =
                        new ArgumentValue(
                            argument.Type,
                            ErrorBuilder.New()
                                .SetMessage(ex.Message)
                                .AddLocation(fieldInfo.Selection)
                                .SetExtension(_argumentProperty, argument.Name)
                                .Build());
                }
            }
        }

        private static void CoerceArgumentValue(
            FieldInfo fieldInfo,
            IInputField argument,
            IDictionary<string, IValueNode> argumentValues)
        {
            if (argumentValues.TryGetValue(argument.Name,
                out IValueNode literal))
            {
                if (literal is VariableNode variable)
                {
                    if (fieldInfo.VarArguments == null)
                    {
                        fieldInfo.VarArguments =
                            new Dictionary<NameString, VariableValue>();
                    }

                    fieldInfo.VarArguments[argument.Name] =
                        new VariableValue(
                            argument.Type,
                            variable.Name.Value,
                            ParseLiteral(argument.Type, argument.DefaultValue));
                }
                else
                {
                    CreateArgumentValue(
                        fieldInfo,
                        argument,
                        literal);
                }
            }
            else
            {
                CreateArgumentValue(
                    fieldInfo,
                    argument,
                    argument.DefaultValue);
            }
        }

        private static void CreateArgumentValue(
            FieldInfo fieldInfo,
            IInputField argument,
            IValueNode literal)
        {
            if (fieldInfo.Arguments == null)
            {
                fieldInfo.Arguments =
                    new Dictionary<NameString, ArgumentValue>();
            }

            object value = ParseLiteral(argument.Type, literal);

            fieldInfo.Arguments[argument.Name] = new ArgumentValue(
                argument.Type,
                value);

            IError error = InputTypeNonNullCheck.CheckForNullValueViolation(
                argument.Name,
                argument.Type,
                value,
                message => ErrorBuilder.New()
                    .SetMessage(message)
                    .AddLocation(fieldInfo.Selection)
                    .SetExtension(_argumentProperty, argument.Name)
                    .Build());

            if (error != null)
            {
                fieldInfo.Arguments[argument.Name] =
                    new ArgumentValue(
                        argument.Type,
                        error);
            }
        }

        private static object ParseLiteral(
            IInputType argumentType,
            IValueNode value)
        {
            IInputType type = (argumentType is NonNullType)
                ? (IInputType)argumentType.InnerType()
                : argumentType;
            return type.ParseLiteral(value);
        }
    }
}
