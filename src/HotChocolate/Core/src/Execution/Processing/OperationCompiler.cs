using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
#if NET6_0_OR_GREATER
using static System.Runtime.InteropServices.CollectionsMarshal;
#endif
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
    private static readonly ImmutableList<ISelectionSetOptimizer> _emptyOptimizers =
        ImmutableList<ISelectionSetOptimizer>.Empty;
    private readonly InputParser _parser;
    private readonly CreateFieldPipeline _createFieldPipeline;
    private readonly Stack<BacklogItem> _backlog = new();
    private readonly Dictionary<Selection, SelectionSetInfo[]> _selectionLookup = new();
    private readonly Dictionary<SelectionSetRef, int> _selectionSetIdLookup = new();
    private readonly Dictionary<int, SelectionVariants> _selectionVariants = new();
    private readonly Dictionary<string, FragmentDefinitionNode> _fragmentDefinitions = new(Ordinal);
    private readonly Dictionary<string, object?> _contextData = new();
    private readonly List<IOperationOptimizer> _operationOptimizers = new();
    private readonly List<ISelectionSetOptimizer> _selectionSetOptimizers = new();
    private readonly List<Selection> _selections = new();
    private readonly HashSet<string> _directiveNames = new(Ordinal);
    private readonly List<FieldMiddleware> _pipelineComponents = new();
    private IncludeCondition[] _includeConditions = Array.Empty<IncludeCondition>();
    private CompilerContext? _deferContext;
    private int _nextSelectionId;
    private int _nextSelectionSetRefId;
    private int _nextSelectionSetId;
    private int _nextFragmentId;
    private bool _hasIncrementalParts;

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

    public IOperation Compile(
        string operationId,
        OperationDefinitionNode operationDefinition,
        ObjectType operationType,
        DocumentNode document,
        ISchema schema,
        IReadOnlyList<IOperationCompilerOptimizer>? optimizers = null)
    {
        if (string.IsNullOrEmpty(operationId))
        {
            throw new ArgumentException(
                OperationCompiler_OperationIdNullOrEmpty,
                nameof(operationId));
        }

        if (operationDefinition is null)
        {
            throw new ArgumentNullException(nameof(operationDefinition));
        }

        if (operationType is null)
        {
            throw new ArgumentNullException(nameof(operationType));
        }

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        try
        {
            // prepare optimizers
            PrepareOptimizers(optimizers);

            var rootOptimizers = _emptyOptimizers;

            if (_selectionSetOptimizers.Count > 0)
            {
                rootOptimizers = ImmutableList.CreateRange(_selectionSetOptimizers);
            }

            // collect root fields
            var rootPath = SelectionPath.Root;
            var id = GetOrCreateSelectionSetRefId(operationDefinition.SelectionSet, rootPath);
            var variants = GetOrCreateSelectionVariants(id);
            SelectionSetInfo[] infos = { new(operationDefinition.SelectionSet, 0) };

            var context = new CompilerContext(schema, document);
            context.Initialize(operationType, variants, infos, rootPath, rootOptimizers);
            CompileSelectionSet(context);

            // process consecutive selections
            while (_backlog.Count > 0)
            {
                var current = _backlog.Pop();
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
            _operationOptimizers.Clear();
            _selectionSetOptimizers.Clear();
            _selections.Clear();
            _directiveNames.Clear();
            _pipelineComponents.Clear();

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
                operationId,
                document,
                operationDefinition,
                schema,
                operationType,
                variants,
                _includeConditions,
                _contextData,
                _hasIncrementalParts,
                _createFieldPipeline);

            foreach (var item in _selectionVariants)
            {
                variants[item.Key] = item.Value;
            }

#if NET5_0_OR_GREATER
            ref var optSpace = ref GetReference(AsSpan(_operationOptimizers));

            for (var i = 0; i < _operationOptimizers.Count; i++)
            {
                Unsafe.Add(ref optSpace, i).OptimizeOperation(context);
            }
#else
            for (var i = 0; i < _operationOptimizers.Count; i++)
            {
                _operationOptimizers[i].OptimizeOperation(context);
            }
#endif

            CompleteResolvers(schema);

            ref var varSpace = ref GetReference(variants.AsSpan());

            for (var i = 0; i < _operationOptimizers.Count; i++)
            {
                Unsafe.Add(ref varSpace, i).Seal();
            }
        }

        return new Operation(
            operationId,
            document,
            operationDefinition,
            operationType,
            variants,
            _includeConditions,
            new Dictionary<string, object?>(_contextData),
            _hasIncrementalParts);
    }

    private void CompleteResolvers(ISchema schema)
    {
#if NET6_0_OR_GREATER
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

#else
        foreach (var selection in _selections)
        {
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
#endif
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
            var selectionSetId = -1;

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

                var selectionPath = context.Path.Append(selection.ResponseName);
                selectionSetId = GetOrCreateSelectionSetRefId(selection.SelectionSet, selectionPath);
                var selectionVariants = GetOrCreateSelectionVariants(selectionSetId);
                var possibleTypes = context.Schema.GetPossibleTypes(fieldType);

                for (var i = possibleTypes.Count - 1; i >= 0; i--)
                {
                    var objectType = possibleTypes[i];

                    if (!selectionVariants.ContainsSelectionSet(objectType))
                    {
                        _backlog.Push(
                            new BacklogItem(
                                objectType,
                                selectionSetId,
                                selection,
                                selectionPath,
                                ResolveOptimizers(context.Optimizers, selection.Field)));
                    }
                }

                // We are waiting for the latest stream and defer spec discussions to be codified
                // before we change the overall stream handling.
                //
                // For now we only allow streams on lists of composite types.
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
                    isParallelExecutable: field.IsParallelExecutable,
                    arguments: CoerceArgumentValues(field, selection, responseName),
                    includeConditions: includeCondition == 0 ? null : new[] { includeCondition });

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

                var id = GetOrCreateSelectionSetRefId(selectionSet, context.Path);
                var variants = GetOrCreateSelectionVariants(id);
                var infos = new SelectionSetInfo[] { new(selectionSet, includeCondition) };

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

                // if we have if condition flags there will be a runtime validation if something
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

    private int GetOrCreateSelectionSetRefId(SelectionSetNode selectionSet, SelectionPath path)
    {
        var selectionSetRef = new SelectionSetRef(selectionSet, path);

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

    internal void RegisterNewSelection(Selection newSelection)
    {
        if (newSelection.SyntaxNode.SelectionSet is not null)
        {
            var selectionSetInfo = new SelectionSetInfo(newSelection.SelectionSet!, 0);
            _selectionLookup.Add(newSelection, new[] { selectionSetInfo });
        }
    }

    internal sealed class SelectionPath : IEquatable<SelectionPath>
    {
        private SelectionPath(string name, SelectionPath? parent = null)
        {
            Name = name;
            Parent = parent;
        }

        public string Name { get; }

        public SelectionPath? Parent { get; }

        public static SelectionPath Root { get; } = new("$root");

        public SelectionPath Append(string name) => new(name, this);

        public bool Equals(SelectionPath? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Name.EqualsOrdinal(other.Name))
            {
                if (ReferenceEquals(Parent, other.Parent))
                {
                    return true;
                }

                return Equals(Parent, other.Parent);
            }

            return false;
        }

        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj) || (obj is SelectionPath other && Equals(other));

        public override int GetHashCode()
            => HashCode.Combine(Name, Parent);
    }

    private readonly struct SelectionSetRef : IEquatable<SelectionSetRef>
    {
        public SelectionSetRef(SelectionSetNode selectionSet, SelectionPath path)
        {
            SelectionSet = selectionSet;
            Path = path;
        }

        public SelectionSetNode SelectionSet { get; }

        public SelectionPath Path { get; }

        public bool Equals(SelectionSetRef other)
            => SelectionSet.Equals(other.SelectionSet, SyntaxComparison.Syntax) &&
                Path.Equals(other.Path);

        public override bool Equals(object? obj)
            => obj is SelectionSetRef other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(SelectionSet, Path);
    }
}
