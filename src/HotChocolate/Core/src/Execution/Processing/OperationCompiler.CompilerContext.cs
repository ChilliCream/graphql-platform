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

        public IObjectType Type { get; private set; } = default!;

        public SelectionSetInfo[] SelectionInfos { get; private set; } = default!;

        public Dictionary<string, Selection> Fields { get; } =
            new(Ordinal);

        public List<IFragment> Fragments { get; } = new();

        public SelectionVariants SelectionVariants { get; private set; } = default!;

        public IImmutableList<ISelectionOptimizer> Optimizers { get; private set; } =
            ImmutableList<ISelectionOptimizer>.Empty;

        public void Initialize(
            IObjectType type,
            SelectionVariants selectionVariants,
            SelectionSetInfo[] selectionInfos,
            IImmutableList<ISelectionOptimizer>? optimizers = null)
        {
            Type = type;
            SelectionVariants = selectionVariants;
            SelectionInfos = selectionInfos;
            Optimizers = optimizers ?? ImmutableList<ISelectionOptimizer>.Empty;
            Fields.Clear();
            Fragments.Clear();
        }
    }
}
