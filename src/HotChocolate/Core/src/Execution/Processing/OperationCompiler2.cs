using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler2
{
    private readonly ISchema _schema;
    private readonly FragmentCollection _fragments;
    private readonly InputParser _parser;
    private readonly Stack<BacklogItem> _backlog = new();
    private readonly Dictionary<Selection2, SelectionSetInfo[]> _selectionLookup = new();
    private readonly Dictionary<SelectionSetNode, int> _selectionSetIdLookup =
        new(SyntaxComparer.BySyntax);
    private readonly Dictionary<int, SelectionVariants2> _selectionVariants = new();
    private IncludeCondition[] _includeConditions = Array.Empty<IncludeCondition>();
    private int _nextSelectionId;
    private int _nextSelectionSetId;
    private int _nextFragmentId;

    internal OperationCompiler2(ISchema schema, FragmentCollection fragments, InputParser parser)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public void Compile(ObjectType operationType, OperationDefinitionNode operationDefinition)
    {
        try
        {
            // collect root fields
            var selectionSetId = GetOrCreateSelectionSetId(operationDefinition.SelectionSet);
            SelectionSetInfo[] selectionSetInfos = { new(operationDefinition.SelectionSet, 0) };

            var context = new CompilerContext();
            context.Initialize(operationType, selectionSetId, selectionSetInfos);
            CompileSelectionSet(context);

            // process consecutive selections
            while (_backlog.Count > 0)
            {
                BacklogItem current = _backlog.Pop();
                selectionSetInfos = _selectionLookup[current.Selection];
                context.Initialize(current.Type,  current.SelectionSetId, selectionSetInfos);
                CompileSelectionSet(context);
            }
        }
        finally
        {
            _nextSelectionId = 0;
            _nextSelectionSetId = 0;
            _nextFragmentId = 0;

            _selectionSetIdLookup.Clear();
            _backlog.Clear();
            _selectionLookup.Clear();
            _selectionVariants.Clear();
            _includeConditions = Array.Empty<IncludeCondition>();
        }
    }

    private void CompileSelectionSet(CompilerContext context)
    {
        // We first collect the fields that we find in the selection set ...
        CollectFields(context);

        // next we will call the selection set optimizers to rewrite the
        // selection set if necessary.
        // OptimizeSelectionSet(context);

        // after that we start completing the selections and build the SelectionSet from
        // the completed selections.
        CompleteSelectionSet(context);
    }

    private void CompleteSelectionSet(CompilerContext context)
    {
        SelectionVariants2 selectionVariants;
        var selections = new ISelection2[context.Fields.Values.Count];
        var fragments = context.Fragments.Count is 0
            ? Array.Empty<IFragment2>()
            : new IFragment2[context.Fragments.Count];
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
                    throw ThrowHelper.QueryCompiler_CompositeTypeSelectionSet(selection.SyntaxNode);
                }

                selectionSetId = GetOrCreateSelectionSetId(selection.SelectionSet);
                selectionVariants = GetOrCreateSelectionVariants(selectionSetId);
                var possibleTypes = _schema.GetPossibleTypes(fieldType);

                for (var i = possibleTypes.Count - 1; i >= 0; i--)
                {
                    var objectType = possibleTypes[i];
                    if (selectionVariants.ContainsSelectionSet(objectType))
                    {
                        _backlog.Push(new BacklogItem(objectType, selectionSetId, selection));
                    }
                }
            }

            // we now seal the selection to make it immutable.
            selection.Seal(selectionSetId);
            selections[selectionIndex++] = selection;
        }

        if (context.Fragments.Count > 0)
        {
            for (var i = 0; i < context.Fragments.Count; i++)
            {
                fragments[i] = context.Fragments[i];
            }
        }

        selectionVariants = GetOrCreateSelectionVariants(context.SelectionSetId);
        selectionVariants.AddSelectionSet(context.Type, selections, fragments, isConditional);
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
                /*
                ResolveInlineFragment(
                    context,
                    (InlineFragmentNode)selection,
                    includeCondition);
                    */
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

        if (context.Type.Fields.TryGetField(fieldName, out IObjectField? field))
        {
            IType fieldType = field.Type.RewriteNullability(selection.Required);

            if (context.Fields.TryGetValue(responseName, out Selection2? preparedSelection))
            {
                preparedSelection.AddSelection(selection, includeCondition);

                if (selection.SelectionSet is not null)
                {
                    var selectionSetInfo = new SelectionSetInfo(
                        selection.SelectionSet!,
                        includeCondition);
                    SelectionSetInfo[] selectionInfos = _selectionLookup[preparedSelection];
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
                preparedSelection = new Selection2(
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
                    // FIX: selection must be bound later
                    resolverPipeline: CreateFieldMiddleware(field, selection),
                    pureResolver: TryCreatePureField(field, selection),
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

    private void ResolveFragmentSpread(
        CompilerContext context,
        FragmentSpreadNode fragmentSpread,
        long includeCondition)
    {
        if (_fragments.GetFragment(fragmentSpread.Name.Value) is { } fragmentInfo &&
            DoesTypeApply(fragmentInfo.TypeCondition, context.Type))
        {
            FragmentDefinitionNode fragmentDefinition = fragmentInfo.FragmentDefinition!;
            includeCondition = GetSelectionIncludeCondition(fragmentSpread, includeCondition);

            if (fragmentDefinition.SelectionSet.Selections.Count == 0)
            {
                throw OperationCompiler_FragmentNoSelections(fragmentDefinition);
            }

            if (fragmentSpread.IsDeferrable())
            {
                var selectionSetId = GetOrCreateSelectionSetId(fragmentDefinition.SelectionSet);
                var selectionVariants = GetOrCreateSelectionVariants(selectionSetId);
                SelectionSetInfo[] selectionSetInfos = { new(fragmentDefinition.SelectionSet, 0) };

                var deferContext = new CompilerContext();
                deferContext.Initialize(context.Type, selectionSetId, selectionSetInfos);
                CompileSelectionSet(deferContext);

                context.Fragments.Add(new Fragment2(
                    GetNextFragmentId(),
                    context.Type,
                    fragmentSpread,
                    fragmentDefinition.Directives,
                    selectionVariants.GetSelectionSet(context.Type),
                    includeCondition));
            }
            else
            {
                CollectFields(context, fragmentDefinition.SelectionSet, includeCondition);
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

    private int GetNextSelectionId() => _nextSelectionId++;

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

    private SelectionVariants2 GetOrCreateSelectionVariants(int selectionSetId)
    {
        if (!_selectionVariants.TryGetValue(selectionSetId, out SelectionVariants2? variants))
        {
            variants = new SelectionVariants2(selectionSetId);
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
                throw new InvalidOperationException(
                    "The operation compiler only allows for 64 unique include conditions.");
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

    private class CompilerContext
    {
        public IObjectType Type { get; private set; } = default!;

        public SelectionSetInfo[] SelectionInfos { get; private set; } = default!;

        public Dictionary<string, Selection2> Fields { get; } =
            new(StringComparer.Ordinal);

        public List<IFragment2> Fragments { get; } = new();

        public int SelectionSetId { get; private set; }

        public void Initialize(
            IObjectType type,
            int selectionSetId,
            SelectionSetInfo[] selectionInfos)
        {
            Type = type;
            SelectionSetId = selectionSetId;
            SelectionInfos = selectionInfos;
            Fields.Clear();
            Fragments.Clear();
        }
    }

    private readonly struct BacklogItem
    {
        public BacklogItem(IObjectType type, int selectionSetId, Selection2 selection)
        {
            Type = type;
            SelectionSetId = selectionSetId;
            Selection = selection;
        }

        public IObjectType Type { get; }

        public int SelectionSetId { get; }

        public Selection2 Selection { get; }
    }

    private readonly struct SelectionSetInfo
    {
        public SelectionSetInfo(SelectionSetNode selectionSet, long includeCondition)
        {
            SelectionSet = selectionSet;
            IncludeCondition = includeCondition;
        }

        public SelectionSetNode SelectionSet { get; }

        public long IncludeCondition { get; }
    }
}
