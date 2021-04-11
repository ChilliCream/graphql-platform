using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing
{
    public sealed partial class OperationCompiler
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

        public static IPreparedOperation Compile(
            string operationId,
            DocumentNode document,
            OperationDefinitionNode operation,
            ISchema schema,
            ObjectType rootType,
            IEnumerable<ISelectionOptimizer>? optimizers = null)
        {
            if (operationId == null)
            {
                throw new ArgumentNullException(nameof(operationId));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (rootType == null)
            {
                throw new ArgumentNullException(nameof(rootType));
            }

            if (operation.SelectionSet.Selections.Count == 0)
            {
                throw OperationCompiler_NoOperationSelections(operation);
            }

            var fragments = new FragmentCollection(schema, document);
            var compiler = new OperationCompiler(schema, fragments);
            var selectionSetLookup = new Dictionary<SelectionSetNode, SelectionVariants>();
            var backlog = new Stack<CompilerContext>();

            // creates and enqueues the root compiler context.
            CompilerContext.New(
                backlog,
                rootType,
                operation.SelectionSet,
                optimizers?.ToImmutableList() ?? ImmutableList<ISelectionOptimizer>.Empty,
                selectionSetLookup);

            // processes the backlog and by doing so traverses the query graph.
            compiler.Visit(backlog);

            return new Operation(operationId, document, operation, rootType, selectionSetLookup);
        }

        private void Visit(Stack<CompilerContext> backlog)
        {
            while (backlog.Count > 0)
            {
                CompileSelectionSet(backlog.Pop());
            }
        }

        private void CompileSelectionSet(CompilerContext context)
        {
            // We first collect the fields that we find in the selection set ...
            CollectFields(context, context.SelectionSet, null);

            // next we will call the selection set optimizers to rewrite the
            // selection set if necessary.
            OptimizeSelectionSet(context);

            // after that we start completing the selections and build the SelectionSet from
            // the completed selections.
            CompleteSelectionSet(context);
        }

        private void CompleteSelectionSet(CompilerContext context)
        {
            foreach (Selection selection in context.Fields.Values)
            {
                // we now mark the selection read-only and add it to the final selection-set.
                selection.MakeReadOnly();
                context.Selections.Add(selection);

                // if one selection of a selection-set is conditional,
                // then the whole set is conditional and has to be post processed during execution.
                if (!context.IsConditional && (selection.IsConditional || selection.IsInternal))
                {
                    context.IsConditional = true;
                }

                // if the field of the selection returns a composite type we will traverse
                // the child selection-sets as well.
                INamedType fieldType = selection.Field.Type.NamedType();
                if (fieldType.IsCompositeType())
                {
                    if (selection.SelectionSet is null)
                    {
                        // composite fields always have to have a selection-set
                        // otherwise we need to throw.
                        throw QueryCompiler_CompositeTypeSelectionSet(selection.SyntaxNode);
                    }

                    IReadOnlyList<ObjectType> possibleTypes = _schema.GetPossibleTypes(fieldType);

                    for (var i = possibleTypes.Count - 1; i >= 0; i--)
                    {
                        // we branch the context which will enqueue the new context
                        // to the work backlog.
                        context.TryBranch(possibleTypes[i], selection);
                    }
                }
            }

            context.Complete();
        }

        private void CollectFields(
            CompilerContext context,
            SelectionSetNode selectionSet,
            SelectionIncludeCondition? includeCondition)
        {
            for (var i = 0; i < selectionSet.Selections.Count; i++)
            {
                ISelectionNode selection = selectionSet.Selections[i];
                SelectionIncludeCondition? selectionCondition = includeCondition;

                if (selectionCondition is null)
                {
                    var reference = new SelectionReference(context.SelectionPath, selection);
                    context.IncludeConditionLookup.TryGetValue(reference, out selectionCondition);
                }

                ResolveFields(
                    context,
                    selection,
                    ExtractVisibility(selection, selectionCondition));
            }
        }


        private void ResolveFields(
            CompilerContext context,
            ISelectionNode selection,
            SelectionIncludeCondition? includeCondition)
        {
            switch (selection.Kind)
            {
                case SyntaxKind.Field:
                    ResolveFieldSelection(
                        context,
                        (FieldNode)selection,
                        includeCondition);
                    break;

                case SyntaxKind.InlineFragment:
                    ResolveInlineFragment(
                        context,
                        (InlineFragmentNode)selection,
                        includeCondition);
                    break;

                case SyntaxKind.FragmentSpread:
                    ResolveFragmentSpread(
                        context,
                        (FragmentSpreadNode)selection,
                        includeCondition);
                    break;
            }
        }

        private void ResolveFieldSelection(
            CompilerContext context,
            FieldNode selection,
            SelectionIncludeCondition? includeCondition)
        {
            NameString fieldName = selection.Name.Value;
            NameString responseName = selection.Alias is null
                ? selection.Name.Value
                : selection.Alias.Value;

            if (context.Type.Fields.TryGetField(fieldName, out IObjectField? field))
            {
                if ((selection.SelectionSet is null ||
                    selection.SelectionSet.Selections.Count == 0) &&
                    field.Type.NamedType().IsCompositeType())
                {
                    throw OperationCompiler_NoCompositeSelections(selection);
                }

                if (context.Fields.TryGetValue(responseName, out Selection? preparedSelection))
                {
                    preparedSelection.AddSelection(selection, includeCondition);
                }
                else
                {
                    // if this is the first time we find a selection to this field we have to
                    // create a new prepared selection.
                    preparedSelection = new Selection(
                        context.Type,
                        field,
                        selection.SelectionSet is not null
                            ? selection.WithSelectionSet(
                                selection.SelectionSet.WithSelections(
                                    selection.SelectionSet.Selections))
                            : selection,
                        responseName: responseName,
                        resolverPipeline: CreateFieldMiddleware(field, selection),
                        arguments: CoerceArgumentValues(field, selection, responseName),
                        includeCondition: includeCondition,
                        internalSelection: context.IsInternalSelection);

                    context.Fields.Add(responseName, preparedSelection);
                }

                if (includeCondition is not null && selection.SelectionSet is not null)
                {
                    var selectionPath = context.SelectionPath.Append(responseName);

                    for (var i = 0; i < selection.SelectionSet.Selections.Count; i++)
                    {
                        ISelectionNode child = selection.SelectionSet.Selections[i];
                        var reference = new SelectionReference(selectionPath, child);

                        if (!context.IncludeConditionLookup.ContainsKey(reference))
                        {
                            context.IncludeConditionLookup.Add(reference, includeCondition);
                        }
                    }
                }
            }
            else
            {
                throw FieldDoesNotExistOnType(selection, context.Type.Name);
            }
        }

        private void ResolveFragmentSpread(
            CompilerContext context,
            FragmentSpreadNode fragmentSpread,
            SelectionIncludeCondition? includeCondition)
        {
            if (_fragments.GetFragment(fragmentSpread.Name.Value) is { } fragmentInfo &&
                DoesTypeApply(fragmentInfo.TypeCondition, context.Type))
            {
                FragmentDefinitionNode fragmentDefinition = fragmentInfo.FragmentDefinition!;

                if (fragmentDefinition.SelectionSet.Selections.Count == 0)
                {
                    throw OperationCompiler_FragmentNoSelections(fragmentDefinition);
                }

                var reference = new SpreadReference(context.SelectionPath, fragmentSpread);

                if (!context.Spreads.TryGetValue(reference, out var selectionSet))
                {
                    selectionSet = fragmentDefinition.SelectionSet.WithSelections(
                        fragmentDefinition.SelectionSet.Selections);
                    context.Spreads.Add(reference, selectionSet);
                }

                if (fragmentSpread.IsDeferrable() &&
                    AllowFragmentDeferral(context, fragmentSpread, fragmentDefinition))
                {
                    CompilerContext deferContext = context.Branch(selectionSet);
                    CompileSelectionSet(deferContext);

                    context.RegisterFragment(new Fragment(
                        context.Type,
                        fragmentSpread,
                        fragmentDefinition,
                        deferContext.GetSelectionSet(),
                        context.IsInternalSelection,
                        includeCondition));
                }
                else
                {
                    CollectFields(context, selectionSet, includeCondition);
                }
            }
        }

        private void ResolveInlineFragment(
            CompilerContext context,
            InlineFragmentNode inlineFragment,
            SelectionIncludeCondition? includeCondition)
        {
            if (inlineFragment.SelectionSet.Selections.Count == 0)
            {
                throw OperationCompiler_FragmentNoSelections(inlineFragment);
            }

            if (_fragments.GetFragment(context.Type, inlineFragment) is { } fragmentInfo &&
                DoesTypeApply(fragmentInfo.TypeCondition, context.Type))
            {
                var reference = new SpreadReference(context.SelectionPath, inlineFragment);

                if (!context.Spreads.TryGetValue(reference, out var selectionSet))
                {
                    selectionSet = inlineFragment.SelectionSet.WithSelections(
                        inlineFragment.SelectionSet.Selections);
                    context.Spreads.Add(reference, selectionSet);
                }

                if (inlineFragment.IsDeferrable() &&
                    AllowFragmentDeferral(context, inlineFragment))
                {
                    CompilerContext deferContext = context.Branch(selectionSet);
                    CompileSelectionSet(deferContext);

                    context.RegisterFragment(new Fragment(
                        context.Type,
                        inlineFragment,
                        deferContext.GetSelectionSet(),
                        context.IsInternalSelection,
                        includeCondition));
                }
                else
                {
                    CollectFields(
                        context,
                        selectionSet,
                        includeCondition);
                }
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

            if (visibility is not null && visibility.Equals(skip, include))
            {
                return visibility;
            }

            return new SelectionIncludeCondition(skip, include, visibility);
        }

        private static bool DoesTypeApply(IType typeCondition, IObjectType current)
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

        private IReadOnlyDictionary<NameString, ArgumentValue>? CoerceArgumentValues(
            IObjectField field,
            FieldNode selection,
            string responseName)
        {
            if (field.Arguments.Count == 0)
            {
                return null;
            }

            var arguments = new Dictionary<NameString, ArgumentValue>();

            for (var i = 0; i < selection.Arguments.Count; i++)
            {
                ArgumentNode argumentValue = selection.Arguments[i];
                if (field.Arguments.TryGetField(
                    argumentValue.Name.Value,
                    out IInputField? argument))
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
                IInputField argument = field.Arguments[i];
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

        private ArgumentValue CreateArgumentValue(
            string responseName,
            IInputField argument,
            ArgumentNode? argumentValue,
            IValueNode value,
            bool isDefaultValue)
        {
            ArgumentNonNullValidator.ValidationResult validationResult =
                ArgumentNonNullValidator.Validate(argument, value, Path.New(argument.Name));

            if (argumentValue is not null && validationResult.HasErrors)
            {
                return new ArgumentValue(
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
                    return new ArgumentValue(
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
                        return new ArgumentValue(
                            argument,
                            ErrorHelper.ArgumentValueIsInvalid(argumentValue, responseName, ex));
                    }

                    return new ArgumentValue(
                        argument,
                        ErrorHelper.ArgumentDefaultValueIsInvalid(responseName, ex));
                }
            }

            return new ArgumentValue(
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

        private static object? ParseLiteral(IInputField argument, IValueNode value)
        {
            IInputType type = argument.Type is NonNullType
                ? (IInputType)argument.Type.InnerType()
                : argument.Type;

            object? runtimeValue = type.ParseLiteral(value);

            return argument.Formatter is not null
                ? argument.Formatter.OnAfterDeserialize(runtimeValue)
                : runtimeValue;
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
                    && directiveType.HasMiddleware)
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

        private void OptimizeSelectionSet(CompilerContext context)
        {
            if (context.Optimizers.Count == 0)
            {
                return;
            }

            var optimizerContext = new SelectionOptimizerContext
            (
                _schema,
                context.Path,
                context.Type,
                context.SelectionSet,
                context.Fields,
                CreateFieldMiddleware
            );

            if (context.Optimizers.Count == 1)
            {
                context.Optimizers[0].OptimizeSelectionSet(optimizerContext);
                return;
            }

            for (var i = 0; i < context.Optimizers.Count; i++)
            {
                context.Optimizers[i].OptimizeSelectionSet(optimizerContext);
            }
        }

        private bool AllowFragmentDeferral(
            CompilerContext context,
            InlineFragmentNode fragment)
        {
            if (context.Optimizers.Count == 0)
            {
                return true;
            }

            var optimizerContext = new SelectionOptimizerContext
            (
                _schema,
                context.Path,
                context.Type,
                context.SelectionSet,
                context.Fields,
                CreateFieldMiddleware
            );

            if (context.Optimizers.Count == 1)
            {
                return context.Optimizers[0].AllowFragmentDeferral(optimizerContext, fragment);
            }

            for (var i = 0; i < context.Optimizers.Count; i++)
            {
                if (!context.Optimizers[i].AllowFragmentDeferral(optimizerContext, fragment))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AllowFragmentDeferral(
            CompilerContext context,
            FragmentSpreadNode fragmentSpread,
            FragmentDefinitionNode fragmentDefinition)
        {
            if (context.Optimizers.Count == 0)
            {
                return true;
            }

            var optimizerContext = new SelectionOptimizerContext
            (
                _schema,
                context.Path,
                context.Type,
                context.SelectionSet,
                context.Fields,
                CreateFieldMiddleware
            );

            if (context.Optimizers.Count == 1)
            {
                return context.Optimizers[0].AllowFragmentDeferral(
                    optimizerContext, fragmentSpread, fragmentDefinition);
            }

            for (var i = 0; i < context.Optimizers.Count; i++)
            {
                if (!context.Optimizers[i].AllowFragmentDeferral(
                    optimizerContext, fragmentSpread, fragmentDefinition))
                {
                    return false;
                }
            }

            return true;
        }

        private static FieldDelegate Compile(
            FieldDelegate fieldPipeline,
            IReadOnlyList<IDirective> directives)
        {
            FieldDelegate next = fieldPipeline;

            for (var i = directives.Count - 1; i >= 0; i--)
            {
                if (directives[i] is { Type: { HasMiddleware: true } } directive)
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
