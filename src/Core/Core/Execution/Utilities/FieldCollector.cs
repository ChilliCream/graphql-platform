using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ArgumentNonNullValidator;

namespace HotChocolate.Execution
{
    internal sealed class FieldCollector
    {
        private const string _argumentProperty = "argument";

        private readonly FragmentCollection _fragments;
        private readonly Func<ObjectField, FieldNode, FieldDelegate> _factory;
        private readonly ITypeConversion _converter;
        private readonly IReadOnlyList<IArgumentCoercionHandler> _coercionHandlers;
        private readonly Func<IInputField, object, object> _coerceArgumentValue;

        public FieldCollector(
            FragmentCollection fragments,
            Func<ObjectField, FieldNode, FieldDelegate> middlewareFactory,
            ITypeConversion converter,
            IEnumerable<IArgumentCoercionHandler> argumentCoercionHandlers)
        {
            _fragments = fragments
                ?? throw new ArgumentNullException(nameof(fragments));
            _factory = middlewareFactory
                ?? throw new ArgumentNullException(nameof(middlewareFactory));
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));

            _coercionHandlers = argumentCoercionHandlers is null
                ? Array.Empty<IArgumentCoercionHandler>()
                : argumentCoercionHandlers.ToArray();

            _coerceArgumentValue = CoerceArgumentValue;
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
            NameString responseName = fieldSelection.Alias == null
                    ? fieldSelection.Name.Value
                    : fieldSelection.Alias.Value;

            if (type.Fields.TryGetField(fieldName, out ObjectField field))
            {
                if (fields.TryGetValue(responseName, out FieldInfo fieldInfo))
                {
                    AddSelection(fieldInfo, fieldSelection);
                    TryAddFieldVisibility(fieldInfo, fieldVisibility);
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

                    TryAddFieldVisibility(fieldInfo, fieldVisibility);
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

        private static FieldVisibility ExtractVisibility(
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

        private void CoerceArgumentValues(FieldInfo fieldInfo)
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
                            argument,
                            ErrorBuilder.New()
                                .SetMessage(ex.Message)
                                .AddLocation(fieldInfo.Selection)
                                .SetExtension(_argumentProperty, argument.Name)
                                .SetPath(fieldInfo.Path.AppendOrCreate(
                                    fieldInfo.ResponseName))
                                .Build());
                }
            }
        }

        private void CoerceArgumentValue(
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
                            new Dictionary<NameString, ArgumentVariableValue>();
                    }

                    object defaultValue = argument.Type.IsLeafType()
                        ? ParseLiteral(argument.Type, argument.DefaultValue)
                        : argument.DefaultValue;
                    defaultValue = CoerceArgumentValue(argument, defaultValue);

                    fieldInfo.VarArguments[argument.Name] =
                        new ArgumentVariableValue(
                            argument,
                            variable.Name.Value,
                            defaultValue,
                            _coerceArgumentValue);
                }
                else
                {
                    CreateArgumentValue(fieldInfo, argument, literal);
                }
            }
            else
            {
                CreateArgumentValue(fieldInfo, argument, argument.DefaultValue);
            }
        }

        private void CreateArgumentValue(
            FieldInfo fieldInfo,
            IInputField argument,
            IValueNode literal)
        {
            if (fieldInfo.Arguments == null)
            {
                fieldInfo.Arguments = new Dictionary<NameString, ArgumentValue>();
            }

            Report report = ArgumentNonNullValidator.Validate(
                argument.Type,
                literal,
                Path.New(argument.Name));

            if (report.HasErrors)
            {
                IError error = ErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.ArgumentValueBuilder_NonNull,
                        argument.Name,
                        TypeVisualizer.Visualize(report.Type)))
                    .AddLocation(fieldInfo.Selection)
                    .SetExtension(_argumentProperty, report.Path.ToCollection())
                    .SetPath(fieldInfo.Path.AppendOrCreate(
                        fieldInfo.ResponseName))
                    .Build();

                fieldInfo.Arguments[argument.Name] =
                    new ArgumentValue(argument, error);
            }
            else if (argument.Type.IsLeafType() && IsLeafLiteral(literal))
            {
                object coerced = CoerceArgumentValue(argument,
                    ParseLiteral(argument.Type, literal));
                fieldInfo.Arguments[argument.Name] =
                    new ArgumentValue(
                        argument,
                        literal.GetValueKind(),
                        coerced);
            }
            else
            {
                object coerced = CoerceArgumentValue(argument, literal);
                fieldInfo.Arguments[argument.Name] =
                    new ArgumentValue(
                        argument,
                        literal.GetValueKind(),
                        coerced);
            }
        }

        private bool IsLeafLiteral(IValueNode value)
        {
            if (value is ObjectValueNode)
            {
                return false;
            }

            if (value is ListValueNode list)
            {
                for (int i = 0; i < list.Items.Count; i++)
                {
                    if (!IsLeafLiteral(list.Items[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void AddSelection(
            FieldInfo fieldInfo,
            FieldNode fieldSelection)
        {
            if (fieldInfo.Nodes == null)
            {
                fieldInfo.Nodes = new List<FieldNode>();
            }
            fieldInfo.Nodes.Add(fieldSelection);
        }

        private static void TryAddFieldVisibility(
            FieldInfo fieldInfo,
            FieldVisibility fieldVisibility)
        {
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

        private static object ParseLiteral(
            IInputType argumentType,
            IValueNode value)
        {
            IInputType type = (argumentType is NonNullType)
                ? (IInputType)argumentType.InnerType()
                : argumentType;
            return type.ParseLiteral(value);
        }

        private object CoerceArgumentValue(IInputField argument, object value)
        {
            if (_coercionHandlers.Count == 0)
            {
                return value;
            }

            object current = value;

            for (int i = 0; i < _coercionHandlers.Count; i++)
            {
                current = _coercionHandlers[i].CoerceValue(argument, current);
            }

            return current;
        }
    }
}
