using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class FieldCollector
    {
        private const string _argumentProperty = "argument";

        private static readonly IReadOnlyDictionary<NameString, PreparedArgument> _emptyArguments =
            new Dictionary<NameString, PreparedArgument>();

        private readonly ISchema _schema;
        private readonly FragmentCollection _fragments;
        private readonly ITypeConversion _converter;

        public FieldCollector(
            ISchema schema, 
            FragmentCollection fragments, 
            ITypeConversion converter)
        {
            _schema = schema;
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

                    fields.Add(responseName, preparedSelection);
                }
            }
            else
            {
                throw new GraphQLException(ErrorBuilder.New()
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

        private IReadOnlyDictionary<NameString, PreparedArgument> CoerceArgumentValues(
            ObjectField field,
            FieldNode selection,
            string responseName)
        {
            if (selection.Arguments.Count == 0)
            {
                return _emptyArguments;
            }

            var arguments = new Dictionary<NameString, PreparedArgument>();

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

        public FieldDelegate GetOrCreateMiddleware(
            ObjectField field,
            FieldNode selection,
            Func<FieldDelegate> fieldPipeline)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            if (fieldPipeline == null)
            {
                throw new ArgumentNullException(nameof(fieldPipeline));
            }

            FieldDelegate directivePipeline = fieldPipeline.Invoke();

            if (field.ExecutableDirectives.Count > 0
                || selection.Directives.Count > 0)
            {
                IReadOnlyList<IDirective> directives =
                    CollectDirectives(field, selection);

                if (directives.Any())
                {
                    directivePipeline = Compile(
                        directivePipeline,
                        directives);
                }
            }

            return directivePipeline;
        }

        private IReadOnlyList<IDirective> CollectDirectives(
            ObjectField field,
            FieldNode selection)
        {
            var processed = new HashSet<string>();
            var directives = new List<IDirective>();

            CollectTypeSystemDirectives(
                processed,
                directives,
                field);

            CollectQueryDirectives(
                processed,
                directives,
                field,
                selection);

            return directives.AsReadOnly();
        }

        private void CollectQueryDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            ObjectField field,
            FieldNode selection)
        {
            foreach (IDirective directive in
                GetFieldSelectionDirectives(field, selection))
            {
                if (!directive.Type.IsRepeatable
                    && !processed.Add(directive.Name))
                {
                    directives.Remove(
                        directives.First(t => t.Type == directive.Type));
                }
                directives.Add(directive);
            }
        }

        private IEnumerable<IDirective> GetFieldSelectionDirectives(
            ObjectField field,
            FieldNode selection)
        {
            foreach (DirectiveNode directive in selection.Directives)
            {
                if (_schema.TryGetDirectiveType(directive.Name.Value,
                    out DirectiveType directiveType)
                    && directiveType.IsExecutable)
                {
                    yield return Directive.FromDescription(
                        directiveType,
                        new DirectiveDefinition(directive),
                        field);
                }
            }
        }

        private static void CollectTypeSystemDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            ObjectField field)
        {
            foreach (IDirective directive in field.ExecutableDirectives)
            {
                if (!directive.Type.IsRepeatable
                    && !processed.Add(directive.Name))
                {
                    directives.Remove(
                        directives.First(t => t.Type == directive.Type));
                }
                directives.Add(directive);
            }
        }

        private static FieldDelegate Compile(
            FieldDelegate fieldPipeline,
            IReadOnlyList<IDirective> directives)
        {
            FieldDelegate next = fieldPipeline;

            for (int i = directives.Count - 1; i >= 0; i--)
            {
                next = BuildComponent(directives[i], next);
            }

            return next;
        }

        private static FieldDelegate BuildComponent(
            IDirective directive,
            FieldDelegate first)
        {
            FieldDelegate next = first;

            IReadOnlyList<DirectiveMiddleware> components =
                directive.MiddlewareComponents;

            for (int i = components.Count - 1; i >= 0; i--)
            {
                DirectiveDelegate component = components[i].Invoke(next);

                next = context =>
                {
                    if (HasErrors(context.Result))
                    {
                        return Task.CompletedTask;
                    }

                    return component.Invoke(
                        new DirectiveContext(context, directive));
                };
            }

            return next;
        }

        private static bool HasErrors(object result)
        {
            if (result is IError error
                || result is IEnumerable<IError> errors)
            {
                return true;
            }

            return false;
        }
    }
}
