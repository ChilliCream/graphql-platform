using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal sealed class SelectionSetByTypePartitioner(FusionSchemaDefinition schema)
{
    public SelectionSetByTypePartitionerResult Partition(SelectionSetByTypePartitionerInput input)
    {
        var context = new Context(schema, input.SelectionSetIndex, input.SelectionSet.Type);

        // Walk the root selection set with the current scope type
        CollectSelections(
            context,
            context.RootType,
            input.SelectionSet.Node.Selections,
            inheritedDirectives: null,
            suppressSharedWrap: false);

        // Build shared node if any
        SelectionSetNode? sharedNode =
            context.SharedSelections.Count > 0
                ? new SelectionSetNode(context.SharedSelections)
                : null;

        if (sharedNode is not null)
        {
            context.Register(input.SelectionSet.Node, sharedNode);
        }

        // Build per-type nodes in deterministic order (MERGING shared + type selections)
        var byTypeBuilder = ImmutableArray.CreateBuilder<SelectionSetByType>(context.TypeSelections.Count);
        foreach (var t in context.TypeInsertionOrder)
        {
            if (context.TypeSelections.TryGetValue(t, out var bucket) && bucket.Count > 0)
            {
                // Merge: shared selections (if any) first, then the concrete-type selections.
                List<ISelectionNode> merged;

                if (context.SharedSelections.Count > 0)
                {
                    merged = new List<ISelectionNode>(context.SharedSelections.Count + bucket.Count);
                    merged.AddRange(context.SharedSelections); // keep shared order and wrappers
                    merged.AddRange(bucket); // then type-specific entries
                }
                else
                {
                    // No shared — just use the bucket content
                    merged = bucket;
                }

                var selectionSetNode = new SelectionSetNode(merged);
                byTypeBuilder.Add(new SelectionSetByType(t, selectionSetNode));

                context.Register(input.SelectionSet.Node, selectionSetNode);
            }
        }

        return new SelectionSetByTypePartitionerResult(
            sharedNode,
            byTypeBuilder.ToImmutable(),
            context.SelectionSetIndex);
    }

   private void CollectSelections(
    Context context,
    ITypeDefinition scopeType,
    IEnumerable<ISelectionNode> selections,
    IReadOnlyList<DirectiveNode>? inheritedDirectives,
    bool suppressSharedWrap = false)
{
    foreach (var selection in selections)
    {
        switch (selection)
        {
            case FieldNode field:
                // Only wrap individual shared fields if we are NOT suppressing shared wrapping
                if (HasAny(inheritedDirectives) && !suppressSharedWrap)
                {
                    context.SharedSelections.Add(CreateUntypedFragment(inheritedDirectives!, new[] { field }));
                }
                else
                {
                    context.SharedSelections.Add(field);
                }
                break;

            case FragmentSpreadNode spread:
                if (HasAny(inheritedDirectives) && !suppressSharedWrap)
                {
                    context.SharedSelections.Add(CreateUntypedFragment(inheritedDirectives!, new[] { spread }));
                }
                else
                {
                    context.SharedSelections.Add(spread);
                }
                break;

                case InlineFragmentNode ifrag:
                    var hasOwn = ifrag.Directives is { Count: > 0 };
                    var combined = CombineDirectives(inheritedDirectives, ifrag.Directives);

                    // 1) Untyped inline fragment
                    if (ifrag.TypeCondition is null)
                    {
                        if (hasOwn)
                        {
                            // Collect inner shared; pass down combined directives so buckets inherit,
                            // but SUPPRESS per-field shared wrapping here (we will wrap once at the end).
                            var tempShared = new List<ISelectionNode>();
                            var saved = context.SharedSelections;
                            context.SharedSelections = tempShared;

                            CollectSelections(
                                context,
                                scopeType,
                                ifrag.SelectionSet.Selections,
                                combined,
                                suppressSharedWrap: true);

                            context.SharedSelections = saved;

                            if (tempShared.Count > 0)
                            {
                                context.SharedSelections.Add(CreateUntypedFragment(combined!, tempShared));
                            }
                        }
                        else
                        {
                            // No own directives → just propagate current inheritance as-is
                            CollectSelections(context, scopeType, ifrag.SelectionSet.Selections, inheritedDirectives, suppressSharedWrap);
                        }
                        break;
                    }

                    // 2) Typed inline fragment
                    var condName = ifrag.TypeCondition.Name.Value;

                    // If the type condition matches the current scope type → shared path
                    if (context.IsSameType(scopeType, condName))
                    {
                        if (hasOwn)
                        {
                            // Pass down combined directives for bucket inheritance,
                            // but SUPPRESS per-field shared wrapping; wrap once at the end.
                            var tempShared = new List<ISelectionNode>();
                            var saved = context.SharedSelections;
                            context.SharedSelections = tempShared;

                            CollectSelections(
                                context,
                                scopeType,
                                ifrag.SelectionSet.Selections,
                                combined,
                                suppressSharedWrap: true);

                            context.SharedSelections = saved;

                            if (tempShared.Count > 0)
                            {
                                context.SharedSelections.Add(CreateUntypedFragment(combined!, tempShared));
                            }
                        }
                        else
                        {
                            // No own directives → just inherit what we already have
                            CollectSelections(context, scopeType, ifrag.SelectionSet.Selections, inheritedDirectives, suppressSharedWrap);
                        }
                        break;
                    }

                    // Otherwise route based on condition kind
                    switch (context.ResolveTypeCondition(condName))
                    {
                        // Concrete object: route & flatten for that type
                        case TypeConditionResolution.Object(var objType):
                        {
                            var flat = context.FilterAndFlattenForType(objType, ifrag.SelectionSet.Selections);
                            if (flat.Count > 0)
                            {
                                if (hasOwn)
                                {
                                    flat = new List<ISelectionNode>
                                {
                                    CreateUntypedFragment(ifrag.Directives!, flat)
                                };
                                }

                                // IMPORTANT: buckets still inherit the incoming directives (NOT suppressed)
                                context.AddToBucket(objType, flat, inheritedDirectives);
                            }
                            break;
                        }

                        // Interface/union: expand to all possible types
                        case TypeConditionResolution.Abstract(var possibleTypes):
                        {
                            foreach (var pt in possibleTypes)
                            {
                                var flat = context.FilterAndFlattenForType(pt, ifrag.SelectionSet.Selections);
                                if (flat.Count == 0)
                                {
                                    continue;
                                }

                                if (hasOwn)
                                {
                                    flat = new List<ISelectionNode>
                                {
                                    CreateUntypedFragment(ifrag.Directives!, flat)
                                };
                                }

                                // buckets inherit incoming directives (NOT suppressed)
                                context.AddToBucket(pt, flat, inheritedDirectives);
                            }
                            break;
                        }

                        // Unknown: conservative → keep as-is in shared; respect inheritance
                        case TypeConditionResolution.Unknown:
                        default:
                            if (HasAny(inheritedDirectives) && !suppressSharedWrap)
                            {
                                context.SharedSelections.Add(CreateUntypedFragment(inheritedDirectives!, new[] { ifrag }));
                            }
                            else
                            {
                                context.SharedSelections.Add(ifrag);
                            }
                            break;
                    }

                    break;
            }
    }
}

    private static InlineFragmentNode CreateUntypedFragment(
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<ISelectionNode> inner)
        => new InlineFragmentNode(null, null, directives, new SelectionSetNode(inner));

    private static InlineFragmentNode CreateUntypedFragment(
        IReadOnlyList<DirectiveNode> directives,
        IEnumerable<ISelectionNode> inner)
        => CreateUntypedFragment(directives, inner is IReadOnlyList<ISelectionNode> rl ? rl : inner.ToList());

    private static bool HasAny(IReadOnlyList<DirectiveNode>? directives)
        => directives is { Count: > 0 };

    private static IReadOnlyList<DirectiveNode>? CombineDirectives(
        IReadOnlyList<DirectiveNode>? outer,
        IReadOnlyList<DirectiveNode>? inner)
    {
        if (!HasAny(outer))
        {
            return inner;
        }

        if (!HasAny(inner))
        {
            return outer;
        }

        // preserve order: outer first, then inner
        var list = new List<DirectiveNode>(outer!.Count + inner!.Count);
        list.AddRange(outer!);
        list.AddRange(inner!);
        return list;
    }

    private sealed class Context
    {
        private readonly FusionSchemaDefinition _schema;

        public Context(
            FusionSchemaDefinition schema,
            ISelectionSetIndex selectionSetIndex,
            ITypeDefinition rootType)
        {
            _schema = schema;
            SelectionSetIndex = selectionSetIndex;
            RootType = rootType;
        }

        public  ISelectionSetIndex SelectionSetIndex { get; private set; }

        [field: AllowNull, MaybeNull]
        public SelectionSetIndexBuilder SelectionSetIndexBuilder
        {
            get
            {
                if (field is null)
                {
                    field = SelectionSetIndex.ToBuilder();
                    this.SelectionSetIndex = field;
                }

                return field;
            }
        }
        public ITypeDefinition RootType { get; }

        public List<ISelectionNode> SharedSelections { get; set; } = [];
        public Dictionary<FusionObjectTypeDefinition, List<ISelectionNode>> TypeSelections { get; } = [];

        // Deterministic output order
        public List<FusionObjectTypeDefinition> TypeInsertionOrder { get; } = [];

        public void AddToBucket(
            FusionObjectTypeDefinition type,
            IEnumerable<ISelectionNode> nodes,
            IReadOnlyList<DirectiveNode>? inheritedDirectives)
        {
            if (!TypeSelections.TryGetValue(type, out var bucket))
            {
                bucket = [];
                TypeSelections[type] = bucket;
                TypeInsertionOrder.Add(type);
            }

            if (HasAny(inheritedDirectives))
            {
                // Inherit directives at the boundary where nodes enter the concrete bucket:
                // wrap all additions once, preserving shape underneath.
                bucket.Add(CreateUntypedFragment(inheritedDirectives!, nodes));
            }
            else
            {
                bucket.AddRange(nodes);
            }
        }

        public List<ISelectionNode> FilterAndFlattenForType(
            FusionObjectTypeDefinition targetType,
            IEnumerable<ISelectionNode> nodes)
        {
            var result = new List<ISelectionNode>();

            foreach (var node in nodes)
            {
                switch (node)
                {
                    case FieldNode f:
                        result.Add(f);
                        break;

                    case FragmentSpreadNode s:
                        result.Add(s);
                        break;

                    case InlineFragmentNode ifrag:
                        // Untyped fragment: flatten; keep its own directives by wrapping once if present.
                        if (ifrag.TypeCondition is null)
                        {
                            var inner = FilterAndFlattenForType(targetType, ifrag.SelectionSet.Selections);
                            if (inner.Count == 0)
                            {
                                break;
                            }

                            if (ifrag.Directives is { Count: > 0 })
                            {
                                result.Add(CreateUntypedFragment(ifrag.Directives, inner));
                            }
                            else
                            {
                                result.AddRange(inner);
                            }
                            break;
                        }

                        var condName = ifrag.TypeCondition.Name.Value;
                        switch (ResolveTypeCondition(condName))
                        {
                            case TypeConditionResolution.Object(var objType):
                                if (ReferenceEquals(objType, targetType))
                                {
                                    var inner = FilterAndFlattenForType(targetType, ifrag.SelectionSet.Selections);
                                    if (inner.Count == 0)
                                    {
                                        break;
                                    }

                                    if (ifrag.Directives is { Count: > 0 })
                                    {
                                        result.Add(CreateUntypedFragment(ifrag.Directives, inner));
                                    }
                                    else
                                    {
                                        result.AddRange(inner);
                                    }
                                }
                                break;

                            case TypeConditionResolution.Abstract(var possibles):
                                if (possibles.Contains(targetType))
                                {
                                    var inner = FilterAndFlattenForType(targetType, ifrag.SelectionSet.Selections);
                                    if (inner.Count == 0)
                                    {
                                        break;
                                    }

                                    if (ifrag.Directives is { Count: > 0 })
                                    {
                                        result.Add(CreateUntypedFragment(ifrag.Directives, inner));
                                    }
                                    else
                                    {
                                        result.AddRange(inner);
                                    }
                                }
                                break;

                            case TypeConditionResolution.Unknown:
                                // Conservative: keep wrapper as-is
                                result.Add(ifrag);
                                break;
                        }
                        break;
                }
            }

            return result;
        }

        public TypeConditionResolution ResolveTypeCondition(string typeName)
        {
            if (!_schema.Types.TryGetType(typeName, out var t))
            {
                return TypeConditionResolution.Unknown.Instance;
            }

            if (t is FusionObjectTypeDefinition o)
            {
                return new TypeConditionResolution.Object(o);
            }

            var possible = _schema.GetPossibleTypes(t);
            return new TypeConditionResolution.Abstract(possible);
        }

        public bool IsSameType(ITypeDefinition scopeType, string typeName)
        {
            return _schema.Types.TryGetType(typeName, out var t) && ReferenceEquals(t, scopeType);
        }

        public void Register(SelectionSetNode original, SelectionSetNode branch)
        {
            if (ReferenceEquals(original, branch))
            {
                return;
            }

            if (SelectionSetIndex.IsRegistered(branch))
            {
                return;
            }

            SelectionSetIndexBuilder.Register(original, branch);
        }
    }

    private abstract record TypeConditionResolution
    {
        public sealed record Object(FusionObjectTypeDefinition Type) : TypeConditionResolution;
        public sealed record Abstract(IReadOnlyList<FusionObjectTypeDefinition> PossibleTypes) : TypeConditionResolution;
        public sealed record Unknown : TypeConditionResolution
        {
            public static readonly Unknown Instance = new();
            private Unknown() { }
        }
    }
}

internal readonly ref struct SelectionSetByTypePartitionerInput
{
    public required SelectionSet SelectionSet { get; init; }
    public required ISelectionSetIndex SelectionSetIndex { get; init; }
}

internal sealed record SelectionSetByTypePartitionerResult(
    SelectionSetNode? SharedSelectionSet,
    ImmutableArray<SelectionSetByType> SelectionSetsByType,
    ISelectionSetIndex SelectionSetIndex);

internal sealed record SelectionSetByType(FusionObjectTypeDefinition Type, SelectionSetNode SelectionSet);
