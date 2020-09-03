using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using PSS = HotChocolate.Execution.Utilities.PreparedSelectionSet;
using static HotChocolate.Execution.Utilities.ThrowHelper;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class OperationCompiler
    {
        private readonly ISchema _schema;
        private readonly FragmentCollection _fragments;

        private OperationCompiler(
            ISchema schema,
            FragmentCollection fragments)
        {
            _schema = schema;
            _fragments = fragments;
        }

        public static IReadOnlyDictionary<SelectionSetNode, PSS> Compile(
            ISchema schema,
            FragmentCollection fragments,
            OperationDefinitionNode operation,
            IEnumerable<ISelectionSetOptimizer>? optimizers = null)
        {
            var selectionSets = new Dictionary<SelectionSetNode, PSS>();
            void Register(PreparedSelectionSet s) => selectionSets[s.SelectionSet] = s;

            SelectionSetNode selectionSet = operation.SelectionSet;
            ObjectType typeContext = schema.GetOperationType(operation.Operation);
            var root = new PSS(operation.SelectionSet);
            Register(root);

            var collector = new OperationCompiler(schema, fragments);

            collector.Visit(
                selectionSet,
                typeContext,
                new Stack<IObjectField>(),
                root,
                Register,
                new Dictionary<ISelectionNode, SelectionIncludeCondition>(),
                optimizers?.ToList() ?? new List<ISelectionSetOptimizer>());

            return selectionSets;
        }

        private void Visit(
            SelectionSetNode selectionSet,
            ObjectType typeContext,
            Stack<IObjectField> fieldContext,
            PSS current,
            Action<PSS> register,
            IDictionary<ISelectionNode, SelectionIncludeCondition> includeConditionLookup,
            List<ISelectionSetOptimizer> optimizers,
            bool internalSelection = false)
        {
            // we first collect the fields that we find in the selection set ...
            IDictionary<string, PreparedSelection> fields =
                CollectFields(typeContext, selectionSet, includeConditionLookup, internalSelection);

            // ... after that is done we will check if there are query optimizer that want
            // to provide additional fields.
            OptimizeSelectionSet(fieldContext, typeContext, selectionSet, fields, optimizers);

            var selections = new List<PreparedSelection>();
            var isConditional = false;

            foreach (PreparedSelection selection in fields.Values)
            {
                // we now make the selection read-only and add it to the final selection-set.
                selection.MakeReadOnly();
                selections.Add(selection);

                // if one selection of a selection-set is conditional,
                // then the whole set is conditional.
                if (!isConditional && (selection.IsConditional || selection.IsInternal))
                {
                    isConditional = true;
                }

                // if the field of the selection returns a composite type we will traverse
                // the child selection-sets as well.
                INamedType fieldType = selection.Field.Type.NamedType();
                if (fieldType.IsCompositeType())
                {
                    if (selection.SelectionSet is null)
                    {
                        // composite fields always have to have a selection-set
                        // otherwithe we need to throw.
                        throw QueryCompiler_CompositeTypeSelectionSet(selection.Selection);
                    }

                    var next = new PSS(selection.SelectionSet);
                    register(next);

                    IReadOnlyList<ObjectType> possibleTypes = _schema.GetPossibleTypes(fieldType);
                    for (var i = 0; i < possibleTypes.Count; i++)
                    {
                        // we prepare the field context and check if there are field specific
                        // optimizers that we might want to include.
                        fieldContext.Push(selection.Field);
                        int initialCount = optimizers.Count;
                        int registered = RegisterOptimizers(optimizers, selection.Field);

                        Visit(
                            selection.SelectionSet,
                            possibleTypes[i],
                            fieldContext,
                            next,
                            register,
                            includeConditionLookup,
                            optimizers,
                            selection.IsInternal);

                        // lets clean up the context again and move on to the next.
                        UnregisterOptimizers(optimizers, initialCount, registered);
                        fieldContext.Pop();
                    }
                }
            }

            current.AddSelections(
                typeContext,
                new PreparedSelectionList(selections, isConditional));
        }

        private IDictionary<string, PreparedSelection> CollectFields(
            ObjectType typeContext,
            SelectionSetNode selectionSet,
            IDictionary<ISelectionNode, SelectionIncludeCondition> includeConditionLookup,
            bool internalSelection)
        {
            var fields = new OrderedDictionary<string, PreparedSelection>();

            CollectFields(
                typeContext,
                selectionSet,
                null,
                includeConditionLookup,
                fields, internalSelection);

            return fields;
        }

        private void CollectFields(
            ObjectType typeContext,
            SelectionSetNode selectionSet,
            SelectionIncludeCondition? includeCondition,
            IDictionary<ISelectionNode, SelectionIncludeCondition> includeConditionLookup,
            IDictionary<string, PreparedSelection> fields,
            bool internalSelection)
        {
            for (var i = 0; i < selectionSet.Selections.Count; i++)
            {
                ISelectionNode selection = selectionSet.Selections[i];
                SelectionIncludeCondition? selectionVisibility = includeCondition;

                if (selectionVisibility is null)
                {
                    includeConditionLookup.TryGetValue(selection, out selectionVisibility);
                }

                ResolveFields(
                    typeContext,
                    selection,
                    ExtractVisibility(selection, selectionVisibility),
                    includeConditionLookup,
                    fields,
                    internalSelection);
            }
        }

        private void ResolveFields(
            ObjectType typeContext,
            ISelectionNode selection,
            SelectionIncludeCondition? includeCondition,
            IDictionary<ISelectionNode, SelectionIncludeCondition> includeConditionLookup,
            IDictionary<string, PreparedSelection> fields,
            bool internalSelection)
        {
            switch (selection.Kind)
            {
                case SyntaxKind.Field:
                    ResolveFieldSelection(
                        typeContext,
                        (FieldNode)selection,
                        includeCondition,
                        includeConditionLookup,
                        fields,
                        internalSelection);
                    break;

                case SyntaxKind.InlineFragment:
                    ResolveInlineFragment(
                        typeContext,
                        (InlineFragmentNode)selection,
                        includeCondition,
                        includeConditionLookup,
                        fields,
                        internalSelection);
                    break;

                case SyntaxKind.FragmentSpread:
                    ResolveFragmentSpread(
                        typeContext,
                        (FragmentSpreadNode)selection,
                        includeCondition,
                        includeConditionLookup,
                        fields,
                        internalSelection);
                    break;
            }
        }

        private void ResolveFieldSelection(
            ObjectType typeContext,
            FieldNode selection,
            SelectionIncludeCondition? includeCondition,
            IDictionary<ISelectionNode, SelectionIncludeCondition> includeConditionLookup,
            IDictionary<string, PreparedSelection> fields,
            bool internalSelection)
        {
            NameString fieldName = selection.Name.Value;
            NameString responseName = selection.Alias is null
                ? selection.Name.Value
                : selection.Alias.Value;

            if (typeContext.Fields.TryGetField(fieldName, out ObjectField? field))
            {
                if (fields.TryGetValue(responseName, out PreparedSelection? preparedSelection))
                {
                    preparedSelection.AddSelection(selection, includeCondition);
                }
                else
                {
                    // if this is the first time we find a selection to this field we have to
                    // create a new prepared selection.
                    preparedSelection = new PreparedSelection(
                        typeContext,
                        field,
                        selection,
                        responseName: responseName,
                        resolverPipeline: CreateFieldMiddleware(field, selection),
                        arguments: CoerceArgumentValues(field, selection, responseName),
                        includeCondition: includeCondition,
                        internalSelection: internalSelection);

                    fields.Add(responseName, preparedSelection);
                }

                if (includeCondition is { } && selection.SelectionSet is { })
                {
                    for (var i = 0; i < selection.SelectionSet.Selections.Count; i++)
                    {
                        ISelectionNode child = selection.SelectionSet.Selections[i];
                        if (!includeConditionLookup.ContainsKey(child))
                        {
                            includeConditionLookup.Add(child, includeCondition);
                        }
                    }
                }
            }
            else
            {
                throw FieldDoesNotExistOnType(selection, typeContext.Name);
            }
        }

        private void ResolveFragmentSpread(
            ObjectType type,
            FragmentSpreadNode fragmentSpread,
            SelectionIncludeCondition? includeCondition,
            IDictionary<ISelectionNode, SelectionIncludeCondition> includeConditionLookup,
            IDictionary<string, PreparedSelection> fields,
            bool internalSelection)
        {
            if (_fragments.GetFragment(fragmentSpread.Name.Value) is { } fragment &&
                DoesTypeApply(fragment.TypeCondition, type))
            {
                CollectFields(
                    type,
                    fragment.SelectionSet,
                    includeCondition,
                    includeConditionLookup,
                    fields,
                    internalSelection);
            }
        }

        private void ResolveInlineFragment(
            ObjectType type,
            InlineFragmentNode inlineFragment,
            SelectionIncludeCondition? includeCondition,
            IDictionary<ISelectionNode, SelectionIncludeCondition> includeConditionLookup,
            IDictionary<string, PreparedSelection> fields,
            bool internalSelection)
        {
            if (_fragments.GetFragment(type, inlineFragment) is { } fragment &&
                DoesTypeApply(fragment.TypeCondition, type))
            {
                CollectFields(
                    type,
                    fragment.SelectionSet,
                    includeCondition,
                    includeConditionLookup,
                    fields,
                    internalSelection);
            }
        }

        private static SelectionIncludeCondition? ExtractVisibility(
            Language.IHasDirectives selection,
            SelectionIncludeCondition? visibility)
        {
            if (selection.Directives.Count == 0)
            {
                return visibility;
            }

            IValueNode? skip = selection.Directives.SkipValue();
            IValueNode? include = selection.Directives.IncludeValue();

            if (skip is null && include is null)
            {
                return visibility;
            }

            if (visibility is { } && visibility.Equals(skip, include))
            {
                return visibility;
            }

            return new SelectionIncludeCondition(skip, include, visibility);
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

        private IReadOnlyDictionary<NameString, PreparedArgument>? CoerceArgumentValues(
            ObjectField field,
            FieldNode selection,
            string responseName)
        {
            if (field.Arguments.Count == 0)
            {
                return null;
            }

            var arguments = new Dictionary<NameString, PreparedArgument>();

            for (var i = 0; i < selection.Arguments.Count; i++)
            {
                ArgumentNode argumentValue = selection.Arguments[i];
                if (field.Arguments.TryGetField(argumentValue.Name.Value, out Argument? argument))
                {
                    arguments[argument.Name.Value] =
                        CreateArgumentValue(
                            responseName,
                            argument,
                            argumentValue,
                            argumentValue.Value,
                            false);
                }
            }

            for (var i = 0; i < field.Arguments.Count; i++)
            {
                Argument argument = field.Arguments[i];
                if (!arguments.ContainsKey(argument.Name))
                {
                    arguments[argument.Name.Value] =
                        CreateArgumentValue(
                            responseName,
                            argument,
                            null,
                            argument.DefaultValue ?? NullValueNode.Default,
                            true);
                }
            }

            return arguments;
        }

        private PreparedArgument CreateArgumentValue(
            string responseName,
            Argument argument,
            ArgumentNode? argumentValue,
            IValueNode value,
            bool isDefaultValue)
        {
            ArgumentNonNullValidator.ValidationResult validationResult =
                ArgumentNonNullValidator.Validate(argument, value, Path.New(argument.Name));

            if (argumentValue is { } && validationResult.HasErrors)
            {
                return new PreparedArgument(
                    argument,
                    ErrorHelper.ArgumentNonNullError(
                        argumentValue,
                        responseName,
                        validationResult));
            }

            if (argument.Type.IsLeafType() && CanBeCompiled(value))
            {
                try
                {
                    return new PreparedArgument(
                        argument,
                        value.GetValueKind(),
                        true,
                        isDefaultValue,
                        ParseLiteral(argument, value),
                        value);
                }
                catch (SerializationException ex)
                {
                    if (argumentValue is not null)
                    {
                        return new PreparedArgument(
                            argument,
                            ErrorHelper.ArgumentValueIsInvalid(argumentValue, responseName, ex));
                    }

                    return new PreparedArgument(
                        argument,
                        ErrorHelper.ArgumentDefaultValueIsInvalid(responseName, ex));
                }
            }

            return new PreparedArgument(
                argument,
                value.GetValueKind(),
                false,
                isDefaultValue,
                null,
                value);
        }

        private static bool CanBeCompiled(IValueNode valueLiteral)
        {
            switch (valueLiteral.Kind)
            {
                case SyntaxKind.Variable:
                case SyntaxKind.ObjectValue:
                    return false;

                case SyntaxKind.ListValue:
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

        private static object? ParseLiteral(Argument argument, IValueNode value)
        {
            IInputType type = (argument.Type is NonNullType)
                ? (IInputType)argument.Type.InnerType()
                : argument.Type;
            object? runtimeValue = type.ParseLiteral(value);
            return argument.Formatter is not null
                ? argument.Formatter.OnAfterDeserialize(runtimeValue)
                : argument;
        }

        private FieldDelegate CreateFieldMiddleware(IObjectField field, FieldNode selection)
        {
            FieldDelegate pipeline = field.Middleware;

            if (field.ExecutableDirectives.Count > 0 || selection.Directives.Count > 0)
            {
                IReadOnlyList<IDirective> directives = CollectDirectives(field, selection);

                if (directives.Count > 0)
                {
                    pipeline = Compile(pipeline, directives);
                }
            }

            return pipeline;
        }

        private IReadOnlyList<IDirective> CollectDirectives(
            IObjectField field,
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
            IObjectField field,
            FieldNode selection)
        {
            foreach (IDirective directive in GetFieldSelectionDirectives(field, selection))
            {
                if (!directive.Type.IsRepeatable && !processed.Add(directive.Name))
                {
                    directives.Remove(directives.First(t => t.Type == directive.Type));
                }
                directives.Add(directive);
            }
        }

        private IEnumerable<IDirective> GetFieldSelectionDirectives(
            IObjectField field,
            FieldNode selection)
        {
            for (var i = 0; i < selection.Directives.Count; i++)
            {
                DirectiveNode directive = selection.Directives[i];
                if (_schema.TryGetDirectiveType(directive.Name.Value,
                    out DirectiveType? directiveType)
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
            IObjectField field)
        {
            for (var i = 0; i < field.ExecutableDirectives.Count; i++)
            {
                IDirective directive = field.ExecutableDirectives[i];
                if (!directive.Type.IsRepeatable && !processed.Add(directive.Name))
                {
                    directives.Remove(directives.First(t => t.Type == directive.Type));
                }
                directives.Add(directive);
            }
        }

        private static int RegisterOptimizers(
            IList<ISelectionSetOptimizer> optimizers,
            IObjectField field)
        {
            int count = 0;

            if (SelectionSetOptimizerHelper.TryGetOptimizers(
                field.ContextData,
                out IReadOnlyList<ISelectionSetOptimizer>? fieldOptimizers))
            {
                foreach (ISelectionSetOptimizer optimizer in fieldOptimizers)
                {
                    if (!optimizers.Contains(optimizer))
                    {
                        optimizers.Add(optimizer);
                        count++;
                    }
                }
            }

            return count;
        }

        private static void UnregisterOptimizers(
            IList<ISelectionSetOptimizer> optimizers,
            int initialCount,
            int registeredOptimizers)
        {
            int last = initialCount - 1;

            for (int i = last + registeredOptimizers; i > last; i--)
            {
                optimizers.RemoveAt(i);
            }
        }

        private void OptimizeSelectionSet(
            Stack<IObjectField> fieldContext,
            IObjectType typeContext,
            SelectionSetNode selectionSet,
            IDictionary<string, PreparedSelection> fields,
            IReadOnlyList<ISelectionSetOptimizer> optimizers)
        {
            if (optimizers.Count > 0)
            {
                var context = new SelectionSetOptimizerContext(
                    _schema,
                    fieldContext,
                    typeContext,
                    selectionSet,
                    fields,
                    CreateFieldMiddleware);

                for (var i = 0; i < optimizers.Count; i++)
                {
                    optimizers[i].Optimize(context);
                }
            }
        }

        private static FieldDelegate Compile(
            FieldDelegate fieldPipeline,
            IReadOnlyList<IDirective> directives)
        {
            FieldDelegate next = fieldPipeline;

            for (var i = directives.Count - 1; i >= 0; i--)
            {
                if (directives[i] is { IsExecutable: true } directive)
                {
                    next = BuildComponent(directive, next);
                }
            }

            return next;
        }

        private static FieldDelegate BuildComponent(IDirective directive, FieldDelegate first)
        {
            FieldDelegate next = first;
            IReadOnlyList<DirectiveMiddleware> components = directive.MiddlewareComponents;

            for (var i = components.Count - 1; i >= 0; i--)
            {
                DirectiveDelegate component = components[i].Invoke(next);

                next = context =>
                {
                    if (HasErrors(context.Result))
                    {
                        return default;
                    }

                    return component.Invoke(new DirectiveContext(context, directive));
                };
            }

            return next;
        }

        private static bool HasErrors(object? result) =>
            result is IError || result is IEnumerable<IError>;
    }
}
