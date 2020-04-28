using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class FieldCollector
    {
        private const string _argumentProperty = "argument";

        private static readonly IReadOnlyDictionary<string, PreparedArgument> _emptyArguments =
            new Dictionary<string, PreparedArgument>();

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
                    preparedSelection = new PreparedSelection(
                        type,
                        field,
                        selection,
                        fields.Count,
                        responseName,
                        null,
                        CoerceArgumentValues(field, selection, responseName));

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

        private IReadOnlyDictionary<string, PreparedArgument> CoerceArgumentValues(
            ObjectField field,
            FieldNode selection,
            string responseName)
        {
            if (selection.Arguments.Count == 0)
            {
                return _emptyArguments;
            }

            var arguments = new Dictionary<string, PreparedArgument>();

            for (var i = 0; i < selection.Arguments.Count; i++)
            {
                ArgumentNode argumentValue = selection.Arguments[i];
                if (field.Arguments.TryGetField(argumentValue.Name.Value, out Argument? argument))
                {
                    arguments[argument.Name] =
                        CreateArgumentValue(
                            responseName,
                            argument,
                            argumentValue);
                }
            }

            return arguments;
        }

        private PreparedArgument CreateArgumentValue(
            string responseName,
            Argument argument,
            ArgumentNode argumentValue)
        {
            ArgumentNonNullValidator.ValidationResult validationResult =
                ArgumentNonNullValidator.Validate(
                    argument, argumentValue.Value, Path.New(argument.Name));

            if (validationResult.HasErrors)
            {
                return new PreparedArgument(
                    argument,
                    ErrorHelper.ArgumentNonNullError(
                        argumentValue,
                        responseName,
                        validationResult));
            }

            if (argument.Type.IsLeafType() && CanBeCompiled(argumentValue.Value))
            {
                try
                {
                    return new PreparedArgument(
                        argument,
                        argumentValue.Value.GetValueKind(),
                        true,
                        ParseLiteral(argument.Type, argumentValue.Value),
                        argumentValue.Value);
                }
                catch (ScalarSerializationException ex)
                {
                    return new PreparedArgument(
                        argument,
                        ErrorHelper.ArgumentValueIsInvalid(
                            argumentValue,
                            responseName,
                            ex));
                }
            }

            return new PreparedArgument(
                argument,
                argumentValue.Value.GetValueKind(),
                false,
                null,
                argumentValue.Value);
        }

        private bool CanBeCompiled(IValueNode valueLiteral)
        {
            switch (valueLiteral.Kind)
            {
                case NodeKind.Variable:
                case NodeKind.ObjectValue:
                    return false;

                case NodeKind.ListValue:
                    ListValueNode list = (ListValueNode)valueLiteral;
                    for (var i = 0; i < list.Items.Count; i++)
                    {
                        if (!CanBeCompiled(list.Items[i]))
                        {
                            return false;
                        }
                    }
                    break;
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
}
