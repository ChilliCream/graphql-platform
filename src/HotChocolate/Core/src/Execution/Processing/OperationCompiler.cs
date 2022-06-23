using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static System.StringComparer;
using static HotChocolate.Execution.Properties.Resources;
using static HotChocolate.Execution.ThrowHelper;
using static HotChocolate.Language.SyntaxComparer;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    private readonly InputParser _parser;
    private readonly Stack<BacklogItem> _backlog = new();
    private readonly Dictionary<Selection, SelectionSetInfo[]> _selectionLookup = new();
    private readonly Dictionary<SelectionSetNode, int> _selectionSetIdLookup = new(BySyntax);
    private readonly Dictionary<int, SelectionVariants> _selectionVariants = new();
    private readonly Dictionary<string, FragmentDefinitionNode> _fragmentDefinitions = new(Ordinal);
    private readonly Dictionary<string, object?> _contextData = new();
    private readonly List<IOperationOptimizer> _operationOptimizers = new();
    private readonly List<ISelectionSetOptimizer> _selectionSetOptimizers = new();
    private readonly List<Selection> _selections = new();
    private IncludeCondition[] _includeConditions = Array.Empty<IncludeCondition>();
    private CompilerContext? _deferContext;
    private int _nextSelectionId;
    private int _nextSelectionSetId;
    private int _nextFragmentId;

    internal OperationCompiler(InputParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public IOperation Compile(
        string operationId,
        OperationDefinitionNode operationDefinition,
        ObjectType operationType,
        DocumentNode document,
        ISchema schema,
        IReadOnlyList<IOperationCompilerOptimizer>? optimizers = null)
    {
        try
        {
            // prepare optimizers
            PrepareOptimizers(optimizers);

            var rootOptimizers = ImmutableList<ISelectionSetOptimizer>.Empty;
            if (_selectionSetOptimizers.Count > 0)
            {
                rootOptimizers = ImmutableList.CreateRange(_selectionSetOptimizers);
            }

            // collect root fields
            var id = GetOrCreateSelectionSetId(operationDefinition.SelectionSet);
            var variants = GetOrCreateSelectionVariants(id);
            SelectionSetInfo[] infos = { new(operationDefinition.SelectionSet, 0) };

            var context = new CompilerContext(schema, document);
            context.Initialize(operationType, variants, infos, rootOptimizers);
            CompileSelectionSet(context);

            // process consecutive selections
            while (_backlog.Count > 0)
            {
                var current = _backlog.Pop();
                variants = GetOrCreateSelectionVariants(current.SelectionSetId);

                if (!variants.ContainsSelectionSet(current.Type))
                {
                    infos = _selectionLookup[current.Selection];
                    context.Initialize(current.Type, variants, infos, current.Optimizers);
                    CompileSelectionSet(context);
                }
            }

            // create operation
            return CreateOperation(
                operationId,
                operationDefinition,
                operationType,
                document,
                schema);
        }
        finally
        {
            _nextSelectionId = 0;
            _nextSelectionSetId = 0;
            _nextFragmentId = 0;

            _backlog.Clear();
            _selectionLookup.Clear();
            _selectionSetIdLookup.Clear();
            _selectionVariants.Clear();
            _fragmentDefinitions.Clear();
            _contextData.Clear();
            _operationOptimizers.Clear();
            _selectionSetOptimizers.Clear();
            _selections.Clear();

            _includeConditions = Array.Empty<IncludeCondition>();
            _deferContext = null;
        }
    }

    private Operation CreateOperation(
        string operationId,
        OperationDefinitionNode operationDefinition,
        ObjectType operationType,
        DocumentNode document,
        ISchema schema)
    {
        var variants = new SelectionVariants[_selectionVariants.Count];

        if (_operationOptimizers.Count == 0)
        {
            CompleteResolvers(schema);

            // if we do not have any optimizers we will copy
            // the variants and seal them in one go.
            foreach (var item in _selectionVariants)
            {
                variants[item.Key] = item.Value;
                item.Value.Seal();
            }
        }
        else
        {
            // if we have optimizers we will first copy the variants to its array,
            // after that we will run the optimizers and give them a chance to do some
            // more mutations on the compiled selection variants.
            // after we have executed all optimizers we will seal the selection variants.
            var context = new OperationOptimizerContext(
                schema,
                operationType,
                variants,
                _contextData);

            foreach (var item in _selectionVariants)
            {
                variants[item.Key] = item.Value;
            }

            for (var i = 0; i < _operationOptimizers.Count; i++)
            {
                _operationOptimizers[i].OptimizeOperation(context);
            }

            CompleteResolvers(schema);

            for (var i = 0; i < variants.Length; i++)
            {
                variants[i].Seal();
            }
        }

        return new Operation(
            operationId,
            document,
            operationDefinition,
            operationType,
            variants,
            _includeConditions,
            new Dictionary<string, object?>(_contextData));
    }

    private void CompleteResolvers(ISchema schema)
    {
        foreach (var selection in _selections)
        {
            if (selection.ResolverPipeline is null && selection.PureResolver is null)
            {
                var field = selection.Field;
                var syntaxNode = selection.SyntaxNode;
                var resolver = CreateFieldMiddleware(schema, field, syntaxNode);
                var pureResolver = TryCreatePureField(field, syntaxNode);
                selection.SetResolvers(resolver, pureResolver);
            }
        }
    }

    private void CompileSelectionSet(CompilerContext context)
    {
        // We first collect the fields that we find in the selection set ...
        CollectFields(context);

        // next we will call the selection set optimizers to rewrite the
        // selection set if necessary.
        OptimizeSelectionSet(context);

        // after that we start completing the selections and build the SelectionSet from
        // the completed selections.
        CompleteSelectionSet(context);
    }

    private void CompleteSelectionSet(CompilerContext context)
    {
        var selections = new Selection[context.Fields.Values.Count];
        var fragments = context.Fragments.Count is 0
            ? Array.Empty<Fragment>()
            : new Fragment[context.Fragments.Count];
        var selectionIndex = 0;
        var isConditional = false;

        foreach (var selection in context.Fields.Values)
        {
            // if the field of the selection returns a composite type we will traverse
            // the child selection-sets as well.
            var fieldType = selection.Type.NamedType();

            if (selection.IsConditional)
            {
                isConditional = true;
            }

            if (fieldType.IsCompositeType())
            {
                if (selection.SelectionSet is null)
                {
                    // composite fields always have to have a selection-set
                    // otherwise we need to throw.
                    throw QueryCompiler_CompositeTypeSelectionSet(selection.SyntaxNode);
                }

                var selectionSetId = GetOrCreateSelectionSetId(selection.SelectionSet);
                var selectionVariants = GetOrCreateSelectionVariants(selectionSetId);
                var possibleTypes = context.Schema.GetPossibleTypes(fieldType);

                for (var i = possibleTypes.Count - 1; i >= 0; i--)
                {
                    var objectType = possibleTypes[i];
                    if (!selectionVariants.ContainsSelectionSet(objectType))
                    {
                        _backlog.Push(new BacklogItem(
                            objectType,
                            selectionSetId,
                            selection,
                            ResolveOptimizers(context.Optimizers, selection.Field)));
                    }
                }
            }

            selections[selectionIndex++] = selection;
            _selections.Add(selection);
        }

        if (context.Fragments.Count > 0)
        {
            for (var i = 0; i < context.Fragments.Count; i++)
            {
                fragments[i] = context.Fragments[i];
            }
        }

        context.SelectionVariants.AddSelectionSet(
            context.Type,
            selections,
            fragments,
            isConditional);
    }

    private void CollectFields(CompilerContext context)
    {
        for (var i = 0; i < context.SelectionInfos.Length; i++)
        {
            var selectionSetInfo = context.SelectionInfos[i];

            CollectFields(
                context,
                selectionSetInfo.SelectionSet,
                selectionSetInfo.IncludeCondition);
        }
    }

    private void CollectFields(
        CompilerContext context,
        SelectionSetNode selectionSet,
        long includeCondition)
    {
        for (var j = 0; j < selectionSet.Selections.Count; j++)
        {
            ResolveFields(context, selectionSet.Selections[j], includeCondition);
        }
    }

    private void ResolveFields(
        CompilerContext context,
        ISelectionNode selection,
        long includeCondition)
    {
        switch (selection.Kind)
        {
            case SyntaxKind.Field:
                ResolveField(
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

    private void ResolveField(
        CompilerContext context,
        FieldNode selection,
        long includeCondition)
    {
        includeCondition = GetSelectionIncludeCondition(selection, includeCondition);

        var fieldName = selection.Name.Value;
        var responseName = selection.Alias?.Value ?? fieldName;

        if (context.Type.Fields.TryGetField(fieldName, out var field))
        {
            var fieldType = field.Type.RewriteNullability(selection.Required);

            if (context.Fields.TryGetValue(responseName, out var preparedSelection))
            {
                preparedSelection.AddSelection(selection, includeCondition);

                if (selection.SelectionSet is not null)
                {
                    var selectionSetInfo = new SelectionSetInfo(
                        selection.SelectionSet!,
                        includeCondition);
                    var selectionInfos = _selectionLookup[preparedSelection];
                    var next = selectionInfos.Length;
                    Array.Resize(ref selectionInfos, next + 1);
                    selectionInfos[next] = selectionSetInfo;
                    _selectionLookup[preparedSelection] = selectionInfos;
                }
            }
            else
            {
                // if this is the first time we find a selection to this field we have to
                // create a new prepared selection.
                preparedSelection = new Selection(
                    GetNextSelectionId(),
                    context.Type,
                    field,
                    fieldType,
                    selection.SelectionSet is not null
                        ? selection.WithSelectionSet(
                            selection.SelectionSet.WithSelections(
                                selection.SelectionSet.Selections))
                        : selection,
                    responseName: responseName,
                    strategy: field.IsParallelExecutable
                        ? SelectionExecutionStrategy.Default
                        : SelectionExecutionStrategy.Serial,
                    arguments: CoerceArgumentValues(field, selection, responseName),
                    includeCondition: includeCondition);

                context.Fields.Add(responseName, preparedSelection);

                if (selection.SelectionSet is not null)
                {
                    var selectionSetInfo = new SelectionSetInfo(
                        selection.SelectionSet!,
                        includeCondition);
                    _selectionLookup.Add(preparedSelection, new[] { selectionSetInfo });
                }
            }
        }
        else
        {
            throw FieldDoesNotExistOnType(selection, context.Type.Name);
        }
    }

    private void ResolveInlineFragment(
        CompilerContext context,
        InlineFragmentNode inlineFragment,
        long includeCondition)
    {
        ResolveFragment(
            context,
            inlineFragment,
            inlineFragment.TypeCondition,
            inlineFragment.SelectionSet,
            inlineFragment.Directives,
            includeCondition);
    }

    private void ResolveFragmentSpread(
        CompilerContext context,
        FragmentSpreadNode fragmentSpread,
        long includeCondition)
    {
        var fragmentDef = GetFragmentDefinition(context, fragmentSpread);

        ResolveFragment(
            context,
            fragmentSpread,
            fragmentDef.TypeCondition,
            fragmentDef.SelectionSet,
            fragmentSpread.Directives,
            includeCondition);
    }

    private void ResolveFragment(
        CompilerContext context,
        ISelectionNode selection,
        NamedTypeNode? typeCondition,
        SelectionSetNode selectionSet,
        IReadOnlyList<DirectiveNode> directives,
        long includeCondition)
    {
        if (typeCondition is null ||
            (context.Schema.TryGetTypeFromAst(typeCondition, out IType typeCon) &&
            DoesTypeApply(typeCon, context.Type)))
        {
            includeCondition |= GetSelectionIncludeCondition(selection, includeCondition);

            if (directives.IsDeferrable())
            {
                var id = GetOrCreateSelectionSetId(selectionSet);
                var variants = GetOrCreateSelectionVariants(id);
                var infos = new SelectionSetInfo[] { new(selectionSet, includeCondition) };

                if (!variants.ContainsSelectionSet(context.Type))
                {
                    var deferContext = RentContext(context);
                    deferContext.Initialize(context.Type, variants, infos);
                    CompileSelectionSet(deferContext);
                    ReturnContext(deferContext);
                }

                var fragment = new Fragment(
                    GetNextFragmentId(),
                    context.Type,
                    selection,
                    directives,
                    selectionSetId: id,
                    variants.GetSelectionSet(context.Type),
                    includeCondition);

                context.Fragments.Add(fragment);
            }
            else
            {
                CollectFields(context, selectionSet, includeCondition);
            }
        }
    }

    private static bool DoesTypeApply(IType typeCondition, IObjectType current)
        => typeCondition.Kind switch
        {
            TypeKind.Object => ReferenceEquals(typeCondition, current),
            TypeKind.Interface => current.IsImplementing((InterfaceType)typeCondition),
            TypeKind.Union => ((UnionType)typeCondition).Types.ContainsKey(current.Name),
            _ => false
        };

    private FragmentDefinitionNode GetFragmentDefinition(
        CompilerContext context,
        FragmentSpreadNode fragmentSpread)
    {
        var fragmentName = fragmentSpread.Name.Value;

        if (!_fragmentDefinitions.TryGetValue(fragmentName, out var value))
        {
            var document = context.Document;

            for (var i = 0; i < document.Definitions.Count; i++)
            {
                if (document.Definitions[i] is FragmentDefinitionNode fragmentDefinition &&
                    fragmentDefinition.Name.Value.EqualsOrdinal(fragmentName))
                {
                    value = fragmentDefinition;
                    _fragmentDefinitions.Add(fragmentName, value);
                    goto EXIT;
                }
            }

            throw new InvalidOperationException(string.Format(
                OperationCompiler_FragmentNotFound,
                fragmentName));
        }

        EXIT:
        return value;
    }

    internal int GetNextSelectionId() => _nextSelectionId++;

    private int GetNextFragmentId() => _nextFragmentId++;

    private int GetOrCreateSelectionSetId(SelectionSetNode selectionSet)
    {
        if (!_selectionSetIdLookup.TryGetValue(selectionSet, out var selectionSetId))
        {
            selectionSetId = _nextSelectionSetId++;
            _selectionSetIdLookup.Add(selectionSet, selectionSetId);
        }
        return selectionSetId;
    }

    private SelectionVariants GetOrCreateSelectionVariants(int selectionSetId)
    {
        if (!_selectionVariants.TryGetValue(selectionSetId, out var variants))
        {
            variants = new SelectionVariants(selectionSetId);
            _selectionVariants.Add(selectionSetId, variants);
        }
        return variants;
    }

    private long GetSelectionIncludeCondition(
        ISelectionNode selectionSyntax,
        long parentIncludeCondition)
    {
        var condition = IncludeCondition.FromSelection(selectionSyntax);

        if (condition.IsDefault)
        {
            return parentIncludeCondition;
        }

        var pos = Array.IndexOf(_includeConditions, condition);

        if (pos == -1)
        {
            pos = _includeConditions.Length;

            if (pos == 64)
            {
                throw new InvalidOperationException(OperationCompiler_ToManyIncludeConditions);
            }

            if (_includeConditions.Length == 0)
            {
                _includeConditions = new IncludeCondition[1];
            }
            else
            {
                Array.Resize(ref _includeConditions, pos + 1);
            }

            _includeConditions[pos] = condition;
        }

        long selectionIncludeCondition = 2 ^ pos;

        if (parentIncludeCondition == 0)
        {
            return selectionIncludeCondition;
        }

        parentIncludeCondition |= selectionIncludeCondition;
        return parentIncludeCondition;
    }

    private CompilerContext RentContext(CompilerContext context)
    {
        if (_deferContext is null)
        {
            return new CompilerContext(context.Schema, context.Document);
        }

        var temp = _deferContext;
        _deferContext = null;
        return temp;
    }

    private void ReturnContext(CompilerContext context)
        => _deferContext ??= context;

    private void PrepareOptimizers(IReadOnlyList<IOperationCompilerOptimizer>? optimizers)
    {
        // we only clear the selectionSetOptimizers since we use this list as a temp
        // to temporarily store the selectionSetOptimizers before they are copied to
        // the context.
        _selectionSetOptimizers.Clear();

        if (optimizers is null)
        {
            return;
        }

        if (optimizers.Count > 0)
        {
            for (var i = 0; i < optimizers.Count; i++)
            {
                var optimizer = optimizers[i];

                if (optimizer is ISelectionSetOptimizer selectionSetOptimizer)
                {
                    _selectionSetOptimizers.Add(selectionSetOptimizer);
                }

                if (optimizer is IOperationOptimizer operationOptimizer)
                {
                    _operationOptimizers.Add(operationOptimizer);
                }
            }
        }
    }
}
