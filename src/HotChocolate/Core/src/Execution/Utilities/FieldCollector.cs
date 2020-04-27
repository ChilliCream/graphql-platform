using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class FieldCollector
    {
        private const string _argumentProperty = "argument";

        private readonly FragmentCollection _fragments;
        private readonly ITypeConversion _converter;

        public FieldCollector(FragmentCollection fragments, ITypeConversion converter)
        {
            _fragments = fragments;
            _converter = converter;
        }

        public IReadOnlyList<IPreparedSelection> CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            Path path)
        {
            var fields = new OrderedDictionary<string, PreparedSelection>();
            CollectFields(type, selectionSet, path, null, fields);
            return fields.Values.ToList();
        }

        private void CollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            Path path,
            FieldVisibility? fieldVisibility,
            IDictionary<string, PreparedSelection> fields)
        {
            for (var i = 0; i < selectionSet.Selections.Count; i++)
            {
                ISelectionNode selection = selectionSet.Selections[i];

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
            FieldVisibility? fieldVisibility,
            IDictionary<string, PreparedSelection> fields)
        {
            switch (selection.Kind)
            {
                case NodeKind.Field:
                    ResolveFieldSelection(
                        type,
                        (FieldNode)selection,
                        path,
                        fieldVisibility,
                        fields);
                    break;

                case NodeKind.InlineFragment:
                    ResolveInlineFragment(
                        type,
                        (InlineFragmentNode)selection,
                        path,
                        fieldVisibility,
                        fields);
                    break;

                case NodeKind.FragmentDefinition:
                    ResolveFragmentSpread(
                        type,
                        (FragmentSpreadNode)selection,
                        path,
                        fieldVisibility,
                        fields);
                    break;
            }
        }

        private void ResolveFieldSelection(
            ObjectType type,
            FieldNode selection,
            Path path,
            FieldVisibility? visibility,
            IDictionary<string, PreparedSelection> fields)
        {
            NameString fieldName = selection.Name.Value;
            NameString responseName = selection.Alias == null
                ? selection.Name.Value
                : selection.Alias.Value;

            if (type.Fields.TryGetField(fieldName, out ObjectField field))
            {
                if (fields.TryGetValue(responseName, out PreparedSelection? preparedSelection))
                {
                    preparedSelection.Selections.Add(selection);

                    if (visibility is { })
                    {
                        preparedSelection.TryAddVariableVisibility(visibility);
                    }
                }
                else
                {
                    CoerceArgumentValues(fieldInfo);

                    preparedSelection = new PreparedSelection(
                        type, field, selection, fields.Count, responseName, null, null);

                    if (visibility is { })
                    {
                        preparedSelection.TryAddVariableVisibility(visibility);
                    }

                    fields.Add(responseName, fieldInfo);
                }
            }
            else
            {
                throw new QueryException(ErrorBuilder.New()
                    .SetMessage(CoreResources.FieldCollector_FieldNotFound)
                    .SetPath(path)
                    .AddLocation(selection)
                    .Build());
            }
        }

        private void ResolveFragmentSpread(
            ObjectType type,
            FragmentSpreadNode fragmentSpread,
            Path path,
            FieldVisibility? fieldVisibility,
            IDictionary<string, PreparedSelection> fields)
        {
            if (_fragments.GetFragment(fragmentSpread.Name.Value) is { } fragment &&
                DoesTypeApply(fragment.TypeCondition, type))
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
            FieldVisibility? fieldVisibility,
            IDictionary<string, PreparedSelection> fields)
        {
            if (_fragments.GetFragment(type, inlineFragment) is { } fragment &&
                DoesTypeApply(fragment.TypeCondition, type))
            {
                CollectFields(
                    type,
                    fragment.SelectionSet,
                    path,
                    fieldVisibility,
                    fields);
            }
        }

        private static FieldVisibility? ExtractVisibility(
            Language.IHasDirectives selection,
            FieldVisibility? visibility)
        {
            if (selection.Directives.Count == 0)
            {
                return visibility;
            }

            IValueNode? skip = selection.Directives.SkipValue();
            IValueNode? include = selection.Directives.IncludeValue();

            if (skip == null && include == null)
            {
                return visibility;
            }

            if (visibility is { } && visibility.Equals(skip, include))
            {
                return visibility;
            }

            return new FieldVisibility(skip, include, visibility);
        }

        private static bool DoesTypeApply(IType typeCondition, ObjectType current)
        {
            switch (typeCondition.Kind)
            {
                case TypeKind.Object:
                    return ReferenceEquals(typeCondition, current);

                case TypeKind.Interface:
                    return current.IsImplementing((InterfaceType)typeCondition);

                case TypeKind.Union:
                    return ((UnionType)typeCondition).Types.ContainsKey(current.Name);

                default:
                    return false;
            }
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
                                .SetPath(fieldInfo.Path.AppendOrCreate(fieldInfo.ResponseName))
                                .Build());
                }
            }
        }

        private void CoerceArgumentValue(
            FieldInfo fieldInfo,
            IInputField argument,
            IDictionary<string, IValueNode> argumentValues)
        {
            IValueNode defaultValueLiteral = argument.DefaultValue ?? NullValueNode.Default;

            if (argumentValues.TryGetValue(argument.Name, out IValueNode? literal))
            {
                if (literal is VariableNode variable)
                {
                    if (fieldInfo.VarArguments == null)
                    {
                        fieldInfo.VarArguments = new Dictionary<NameString, ArgumentVariableValue>();
                    }

                    object defaultValue = argument.Type.IsLeafType()
                        ? ParseLiteral(argument.Type, defaultValueLiteral)
                        : defaultValueLiteral;
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
                CreateArgumentValue(fieldInfo, argument, defaultValueLiteral);
            }
        }

        private void CreateArgumentValue(
            FieldInfo fieldInfo,
            IInputField argument,
            IValueNode literal)
        {

            Report report = ArgumentNonNullValidator.Validate(
                argument,
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

                fieldInfo.Arguments[argument.Name] = new ArgumentValue(argument, error);
            }
            else if (argument.Type.IsLeafType() && IsLeafLiteral(literal))
            {
                object coerced = CoerceArgumentValue(argument, ParseLiteral(argument.Type, literal));
                fieldInfo.Arguments[argument.Name] = new ArgumentValue(argument, literal.GetValueKind(), coerced);
            }
            else
            {
                object coerced = CoerceArgumentValue(argument, literal);
                fieldInfo.Arguments[argument.Name] = new ArgumentValue(argument, literal.GetValueKind(), coerced);
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

    internal sealed class PreparedSelection
        : IPreparedSelection
    {
        private List<FieldVisibility>? _visibilities;

        public PreparedSelection(
            ObjectType declaringType,
            ObjectField field,
            FieldNode selection,
            int responseIndex,
            string responseName,
            FieldDelegate resolverPipeline,
            IReadOnlyDictionary<NameString, PreparedArgument> arguments)
        {
            DeclaringType = declaringType;
            Field = field;
            Selection = selection;
            ResponseIndex = responseIndex;
            ResponseName = responseName;
            ResolverPipeline = resolverPipeline;
            Arguments = arguments;
            Selections.Add(selection);
        }

        /// <inheritdoc />
        public ObjectType DeclaringType { get; }

        /// <inheritdoc />
        public ObjectField Field { get; }

        /// <inheritdoc />
        public FieldNode Selection { get; }

        /// <inheritdoc />
        public List<FieldNode> Selections { get; } = new List<FieldNode>();

        IReadOnlyList<FieldNode> IPreparedSelection.Selections => Selections;

        /// <inheritdoc />
        public int ResponseIndex { get; }

        /// <inheritdoc />
        public string ResponseName { get; }

        /// <inheritdoc />
        public FieldDelegate ResolverPipeline { get; set; }

        /// <inheritdoc />
        public IReadOnlyDictionary<NameString, PreparedArgument> Arguments { get; }

        /// <inheritdoc />
        public bool IsVisible(IVariableValueCollection variables)
        {
            if (_visibilities is null)
            {
                return true;
            }

            for (int i = 0; i < _visibilities.Count; i++)
            {
                if (!_visibilities[i].IsVisible(variables))
                {
                    return false;
                }
            }

            return true;
        }

        public void TryAddVariableVisibility(FieldVisibility visibility)
        {
            _visibilities ??= new List<FieldVisibility>();

            if (_visibilities.Count == 0)
            {
                _visibilities.Add(visibility);
            }

            for (int i = 0; i < _visibilities.Count; i++)
            {
                if (_visibilities[i].Equals(visibility))
                {
                    return;
                }
            }

            _visibilities.Add(visibility);
        }
    }
}
