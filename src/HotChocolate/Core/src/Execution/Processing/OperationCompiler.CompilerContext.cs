using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using static System.StringComparer;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    internal sealed class CompilerContext
    {
        public CompilerContext(ISchema schema, DocumentNode document)
        {
            Schema = schema;
            Document = document;
        }

        public ISchema Schema { get; }

        public DocumentNode Document { get; }

        public ObjectType Type { get; private set; } = default!;

        public SelectionSetInfo[] SelectionInfos { get; private set; } = default!;

        public SelectionPath Path { get; private set; } = SelectionPath.Root;

        public Dictionary<string, Selection> Fields { get; } =
            new(Ordinal);

        public List<Fragment> Fragments { get; } = new();

        public SelectionVariants SelectionVariants { get; private set; } = default!;

        public IImmutableList<ISelectionSetOptimizer> Optimizers { get; private set; } =
            ImmutableList<ISelectionSetOptimizer>.Empty;

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
