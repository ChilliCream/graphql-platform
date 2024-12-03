using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static System.Runtime.InteropServices.CollectionsMarshal;
using static System.Runtime.InteropServices.MemoryMarshal;
using static System.StringComparer;
using static HotChocolate.Execution.Properties.Resources;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The operation compiler will analyze a specific operation of a GraphQL request document
/// and create from it an optimized executable operation tree.
/// </summary>
public sealed partial class OperationCompiler
{
    private readonly InputParser _parser;
    private readonly CreateFieldPipeline _createFieldPipeline;
    private readonly Queue<BacklogItem> _backlog = new();
    private readonly Dictionary<Selection, SelectionSetInfo[]> _selectionLookup = new();
    private readonly Dictionary<SelectionSetRef, int> _selectionSetIdLookup = new();
    private readonly Dictionary<int, SelectionVariants> _selectionVariants = new();
    private readonly Dictionary<string, FragmentDefinitionNode> _fragmentDefinitions = new(Ordinal);
    private readonly Dictionary<string, object?> _contextData = new();
    private readonly List<Selection> _selections = [];
    private readonly HashSet<string> _directiveNames = new(Ordinal);
    private readonly List<FieldMiddleware> _pipelineComponents = [];
    private readonly HashSet<int> _enqueuedSelectionSets = new();
    private IncludeCondition[] _includeConditions = [];
    private CompilerContext? _deferContext;

    private ImmutableArray<IOperationOptimizer> _operationOptimizers =
        ImmutableArray<IOperationOptimizer>.Empty;

    private int _nextSelectionId;
    private int _nextSelectionSetRefId;
    private int _nextSelectionSetId;
    private int _nextFragmentId;
    private bool _hasIncrementalParts;
    private OperationCompilerMetrics _metrics;

    public OperationCompiler(InputParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));

        _createFieldPipeline =
            (schema, field, selection)
                => CreateFieldPipeline(
                    schema,
                    field,
                    selection,
                    _directiveNames,
                    _pipelineComponents);
    }

    internal OperationCompilerMetrics Metrics => _metrics;

    public IOperation Compile(OperationCompilerRequest request)
    {
        if (string.IsNullOrEmpty(request.Id))
        {
            throw new ArgumentException(
                OperationCompiler_OperationIdNullOrEmpty,
                nameof(request.Id));
        }

        try
        {
            var backlogMaxSize = 0;
            var selectionSetOptimizers = request.SelectionSetOptimizers;
            _operationOptimizers = request.OperationOptimizers;

            // collect root fields
            var rootPath = SelectionPath.Root;
            var id = GetOrCreateSelectionSetRefId(request.Definition.SelectionSet, request.RootType.Name, rootPath);
            var variants = GetOrCreateSelectionVariants(id);
            SelectionSetInfo[] infos = [new(request.Definition.SelectionSet, 0)];

            var context = new CompilerContext(request.Schema, request.Document);
            context.Initialize(request.RootType, variants, infos, rootPath, selectionSetOptimizers);
            CompileSelectionSet(context);

            // process consecutive selections
            while (_backlog.Count > 0)
            {
                backlogMaxSize = Math.Max(backlogMaxSize, _backlog.Count);

                var current = _backlog.Dequeue();
                var type = current.Type;
                variants = GetOrCreateSelectionVariants(current.SelectionSetId);

                if (!variants.ContainsSelectionSet(type))
                {
                    infos = _selectionLookup[current.Selection];
                    context.Initialize(type, variants, infos, current.Path, current.Optimizers);
                    CompileSelectionSet(context);
                }
            }

            // create operation
            var operation = CreateOperation(request);

            _metrics = new OperationCompilerMetrics(
                _nextSelectionId,
                _selectionVariants.Count,
                backlogMaxSize);

            return operation;
        }
        finally
        {
            _nextSelectionId = 0;
            _nextSelectionSetRefId = 0;
            _nextSelectionId = 0;
            _nextFragmentId = 0;
            _hasIncrementalParts = false;

            _backlog.Clear();
            _selectionLookup.Clear();
            _selectionSetIdLookup.Clear();
            _selectionVariants.Clear();
            _fragmentDefinitions.Clear();
            _contextData.Clear();
            _selections.Clear();
            _directiveNames.Clear();
            _pipelineComponents.Clear();
            _enqueuedSelectionSets.Clear();

            _operationOptimizers = ImmutableArray<IOperationOptimizer>.Empty;

            _includeConditions = [];
            _deferContext = null;
        }
    }

    private Operation CreateOperation(OperationCompilerRequest request)
    {
        var operation = new Operation(
            request.Id,
            request.Document,
            request.Definition,
            request.RootType,
            request.Schema);

        var variants = new SelectionVariants[_selectionVariants.Count];

        if (_operationOptimizers.Length == 0)
        {
            CompleteResolvers(request.Schema);

            // if we do not have any optimizers we will copy
            // the variants and seal them in one go.
            foreach (var item in _selectionVariants)
            {
                variants[item.Key] = item.Value;
                item.Value.Seal(operation);
            }
        }
        else
        {
            // if we have optimizers we will first copy the variants to its array,
            // after that we will run the optimizers and give them a chance to do some
            // more mutations on the compiled selection variants.
            // after we have executed all optimizers we will seal the selection variants.
            var context = new OperationOptimizerContext(
                request.Id,
                request.Document,
                request.Definition,
                request.Schema,
                request.RootType,
                variants,
                _includeConditions,
                _contextData,
                _hasIncrementalParts,
                _createFieldPipeline);

            foreach (var item in _selectionVariants)
            {
                variants[item.Key] = item.Value;
            }

            // we will complete the selection variants, sets and selections
            // without sealing them so that analyzers in this step can fully
            // inspect them.
            var variantsSpan = variants.AsSpan();
            ref var variantsStart = ref GetReference(variantsSpan);
            ref var variantsEnd = ref Unsafe.Add(ref variantsStart, variantsSpan.Length);

            while (Unsafe.IsAddressLessThan(ref variantsStart, ref variantsEnd))
            {
                variantsStart.Complete(operation);
                variantsStart = ref Unsafe.Add(ref variantsStart, 1)!;
            }

            var optSpan = _operationOptimizers.AsSpan();
            ref var optStart = ref GetReference(optSpan);
            ref var optEnd = ref Unsafe.Add(ref optStart, optSpan.Length);

            while (Unsafe.IsAddressLessThan(ref optStart, ref optEnd))
            {
                optStart.OptimizeOperation(context);
                optStart = ref Unsafe.Add(ref optStart, 1)!;
            }

            CompleteResolvers(request.Schema);

            variantsSpan = variants.AsSpan();
            variantsStart = ref GetReference(variantsSpan)!;
            variantsEnd = ref Unsafe.Add(ref variantsStart, variantsSpan.Length)!;

            while (Unsafe.IsAddressLessThan(ref variantsStart, ref variantsEnd))
            {
                variantsStart.Seal(operation);
                variantsStart = ref Unsafe.Add(ref variantsStart, 1)!;
            }
        }

        operation.Seal(_contextData, variants, _hasIncrementalParts, _includeConditions);
        return operation;
    }

    private void CompleteResolvers(ISchema schema)
    {
        ref var searchSpace = ref GetReference(AsSpan(_selections));

        for (var i = 0; i < _selections.Count; i++)
        {
            var selection = Unsafe.Add(ref searchSpace, i);

            if (selection.ResolverPipeline is null && selection.PureResolver is null)
            {
                var field = selection.Field;
                var syntaxNode = selection.SyntaxNode;
                var resolver = CreateFieldPipeline(
                    schema,
                    field,
                    syntaxNode,
                    _directiveNames,
                    _pipelineComponents);
                var pureResolver = TryCreatePureField(schema, field, syntaxNode);
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
        var fragments = context.Fragments.Count is not 0
            ? new Fragment[context.Fragments.Count]
            : [];
        var selectionIndex = 0;
        var isConditional = false;

        foreach (var selection in context.Fields.Values)
        {
            // if the field of the selection returns a composite type we will traverse
            // the child selection-sets as well.
            var fieldType = selection.Type.NamedType();
            var selectionSetId = -1;

            if (selection.IsConditional)
            {
                isConditional = true;
            }

            // Determines if the type is a composite type.
            if (fieldType.IsType(TypeKind.Object, TypeKind.Interface, TypeKind.Union))
            {
                if (selection.SelectionSet is null)
                {
                    // composite fields always have to have a selection-set
                    // otherwise we need to throw.
                    throw QueryCompiler_CompositeTypeSelectionSet(selection.SyntaxNode);
                }

                var selectionPath = context.Path.Append(selection.ResponseName);
                selectionSetId = GetOrCreateSelectionSetRefId(selection.SelectionSet, fieldType.Name, selectionPath);
                var possibleTypes = context.Schema.GetPossibleTypes(fieldType);

                if (_enqueuedSelectionSets.Add(selectionSetId))
                {
                    for (var i = possibleTypes.Count - 1; i >= 0; i--)
                    {
                        _backlog.Enqueue(
                            new BacklogItem(
                                possibleTypes[i],
                                selectionSetId,
                                selection,
                                selectionPath,
                                ResolveOptimizers(context.Optimizers, selection.Field)));
                    }
                }

                // We are waiting for the latest stream and defer spec discussions to be codified
                // before we change the overall stream handling.
                //
                // For now, we only allow streams on lists of composite types.
                if (selection.SyntaxNode.IsStreamable())
                {
                    var streamDirective = selection.SyntaxNode.GetStreamDirectiveNode();
                    var nullValue = NullValueNode.Default;
                    var ifValue = streamDirective?.GetIfArgumentValueOrDefault() ?? nullValue;
                    long ifConditionFlags = 0;

                    if (ifValue.Kind is not SyntaxKind.NullValue)
                    {
                        var ifCondition = new IncludeCondition(ifValue, nullValue);
                        ifConditionFlags = GetSelectionIncludeCondition(ifCondition, 0);
                    }

                    selection.MarkAsStream(ifConditionFlags);
                    _hasIncrementalParts = true;
                }
            }

            selection.SetSelectionSetId(selectionSetId);
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
            _nextSelectionSetId++,
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
            var fieldType = field.Type;

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
                var id = GetNextSelectionId();

                // if this is the first time we find a selection to this field we have to
                // create a new prepared selection.
                preparedSelection = new Selection.Sealed(
                    id,
                    context.Type,
                    field,
                    fieldType,
                    selection.SelectionSet is not null
                        ? selection.WithSelectionSet(
                            selection.SelectionSet.WithSelections(
                                selection.SelectionSet.Selections))
                        : selection,
                    responseName: responseName,
                    isParallelExecutable: field.IsParallelExecutable,
                    arguments: CoerceArgumentValues(field, selection, responseName),
                    includeConditions: includeCondition == 0
                        ? null
                        : [includeCondition,]);

                context.Fields.Add(responseName, preparedSelection);

                if (selection.SelectionSet is not null)
                {
                    var selectionSetInfo = new SelectionSetInfo(
                        selection.SelectionSet!,
                        includeCondition);
                    _selectionLookup.Add(preparedSelection, [selectionSetInfo,]);
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
        if (typeCondition is null
            || (context.Schema.TryGetTypeFromAst(typeCondition, out IType typeCon)
                && DoesTypeApply(typeCon, context.Type)))
        {
            includeCondition = GetSelectionIncludeCondition(selection, includeCondition);

            if (directives.IsDeferrable())
            {
                var deferDirective = directives.GetDeferDirectiveNode();
                var nullValue = NullValueNode.Default;
                var ifValue = deferDirective?.GetIfArgumentValueOrDefault() ?? nullValue;

                long ifConditionFlags = 0;

                if (ifValue.Kind is not SyntaxKind.NullValue)
                {
                    var ifCondition = new IncludeCondition(ifValue, nullValue);
                    ifConditionFlags = GetSelectionIncludeCondition(ifCondition, includeCondition);
                }

                var typeName = typeCondition?.Name.Value ?? context.Type.Name;
                var id = GetOrCreateSelectionSetRefId(selectionSet, typeName, context.Path);
                var variants = GetOrCreateSelectionVariants(id);
                var infos = new SelectionSetInfo[] { new(selectionSet, includeCondition), };

                if (!variants.ContainsSelectionSet(context.Type))
                {
                    var deferContext = RentContext(context);
                    deferContext.Initialize(context.Type, variants, infos, context.Path);
                    CompileSelectionSet(deferContext);
                    ReturnContext(deferContext);
                }

                var fragment = new Fragment(
                    GetNextFragmentId(),
                    context.Type,
                    selection,
                    directives,
                    variants.GetSelectionSet(context.Type),
                    includeCondition,
                    ifConditionFlags);

                context.Fragments.Add(fragment);
                _hasIncrementalParts = true;

                // if we have if-condition flags there will be a runtime validation if something
                // shall be deferred, so we need to prepare for both cases.
                //
                // this means that we will collect the fields with our if condition flags as
                // if the fragment was not deferred.
                if (ifConditionFlags is not 0)
                {
                    CollectFields(context, selectionSet, ifConditionFlags);
                }
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
            _ => false,
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
                if (document.Definitions[i] is FragmentDefinitionNode fragmentDefinition
                    && fragmentDefinition.Name.Value.EqualsOrdinal(fragmentName))
                {
                    value = fragmentDefinition;
                    _fragmentDefinitions.Add(fragmentName, value);
                    goto EXIT;
                }
            }

            throw new InvalidOperationException(
                string.Format(
                    OperationCompiler_FragmentNotFound,
                    fragmentName));
        }

        EXIT:
        return value;
    }

    internal int GetNextSelectionId() => _nextSelectionId++;

    private int GetNextFragmentId() => _nextFragmentId++;

    private int GetOrCreateSelectionSetRefId(
        SelectionSetNode selectionSet,
        string selectionSetTypeName,
        SelectionPath path)
    {
        var selectionSetRef = new SelectionSetRef(selectionSet, selectionSetTypeName, path);

        if (!_selectionSetIdLookup.TryGetValue(selectionSetRef, out var selectionSetId))
        {
            selectionSetId = _nextSelectionSetRefId++;
            _selectionSetIdLookup.Add(selectionSetRef, selectionSetId);
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

        long selectionIncludeCondition = 1;
        selectionIncludeCondition <<= pos;

        if (parentIncludeCondition == 0)
        {
            return selectionIncludeCondition;
        }

        parentIncludeCondition |= selectionIncludeCondition;
        return parentIncludeCondition;
    }

    private long GetSelectionIncludeCondition(
        IncludeCondition condition,
        long parentIncludeCondition)
    {
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

        long selectionIncludeCondition = 1;
        selectionIncludeCondition <<= pos;

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

    internal void RegisterNewSelection(Selection newSelection)
    {
        if (newSelection.SyntaxNode.SelectionSet is not null)
        {
            var selectionSetInfo = new SelectionSetInfo(newSelection.SelectionSet!, 0);
            _selectionLookup.Add(newSelection, [selectionSetInfo,]);
        }
    }

    private readonly struct SelectionSetRef(
        SelectionSetNode selectionSet,
        string selectionSetTypeName,
        SelectionPath path)
        : IEquatable<SelectionSetRef>
    {
        public SelectionSetNode SelectionSet { get; } = selectionSet;

        public SelectionPath Path { get; } = path;

        public string SelectionSetTypeName { get; } = selectionSetTypeName;

        public bool Equals(SelectionSetRef other)
            => SyntaxComparer.BySyntax.Equals(SelectionSet, other.SelectionSet)
                && Path.Equals(other.Path)
                && Ordinal.Equals(SelectionSetTypeName, other.SelectionSetTypeName);

        public override bool Equals(object? obj)
            => obj is SelectionSetRef other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(
                SyntaxComparer.BySyntax.GetHashCode(SelectionSet),
                Path.GetHashCode(),
                Ordinal.GetHashCode(SelectionSetTypeName));
    }
}
