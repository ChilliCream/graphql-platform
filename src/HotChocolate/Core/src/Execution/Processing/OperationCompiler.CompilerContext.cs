using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using static System.StringComparer;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    internal sealed class CompilerContext(Schema schema, DocumentNode document)
    {
        public Schema Schema { get; } = schema;

        public DocumentNode Document { get; } = document;

        public ObjectType Type { get; private set; } = null!;

        public SelectionSetInfo[] SelectionInfos { get; private set; } = null!;

        public SelectionPath Path { get; private set; } = SelectionPath.Root;

        public Dictionary<string, Selection> Fields { get; } = new(Ordinal);

        public List<Fragment> Fragments { get; } = [];

        public SelectionVariants SelectionVariants { get; private set; } = null!;

        public ImmutableArray<ISelectionSetOptimizer> Optimizers { get; private set; }

        public void Initialize(
            ObjectType type,
            SelectionVariants selectionVariants,
            SelectionSetInfo[] selectionInfos,
            SelectionPath path,
            ImmutableArray<ISelectionSetOptimizer>? optimizers = null)
        {
            Type = type;
            SelectionVariants = selectionVariants;
            SelectionInfos = selectionInfos;
            Path = path;
            Optimizers = optimizers ?? [];
            Fields.Clear();
            Fragments.Clear();
        }
    }
}
