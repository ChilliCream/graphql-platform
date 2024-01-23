using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using static System.StringComparer;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    internal sealed class CompilerContext(ISchema schema, DocumentNode document, bool enableNullBubbling)
    {
        public ISchema Schema { get; } = schema;

        public DocumentNode Document { get; } = document;

        public ObjectType Type { get; private set; } = default!;

        public SelectionSetInfo[] SelectionInfos { get; private set; } = default!;

        public SelectionPath Path { get; private set; } = SelectionPath.Root;

        public Dictionary<string, Selection> Fields { get; } =
            new(Ordinal);

        public List<Fragment> Fragments { get; } = [];

        public SelectionVariants SelectionVariants { get; private set; } = default!;

        public IImmutableList<ISelectionSetOptimizer> Optimizers { get; private set; } =
            ImmutableList<ISelectionSetOptimizer>.Empty;
        
        public bool EnableNullBubbling { get; } = enableNullBubbling;

        public void Initialize(
            ObjectType type,
            SelectionVariants selectionVariants,
            SelectionSetInfo[] selectionInfos,
            SelectionPath path,
            IImmutableList<ISelectionSetOptimizer>? optimizers = null)
        {
            Type = type;
            SelectionVariants = selectionVariants;
            SelectionInfos = selectionInfos;
            Path = path;
            Optimizers = optimizers ?? ImmutableList<ISelectionSetOptimizer>.Empty;
            Fields.Clear();
            Fragments.Clear();
        }
    }
}
