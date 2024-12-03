using System.Buffers;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

internal abstract partial class ResolverNodeBase
{
    /// <summary>
    /// The resolver configuration.
    /// </summary>
    internal readonly ref struct Config
    {
        private readonly bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of <see cref="Config"/>.
        /// </summary>
        /// <param name="subgraphName">
        /// The name of the subgraph on which this request handler executes.
        /// </param>
        /// <param name="document">
        /// The GraphQL request document.
        /// </param>
        /// <param name="parent">
        /// The selection from which the selection set was composed.
        /// </param>
        /// <param name="selectionSet">
        /// The selection set for which this request provides a patch.
        /// </param>
        /// <param name="rootSelections">
        /// The root selections of this subgraph request.
        /// </param>
        /// <param name="provides">
        /// The variables that this resolver node will provide.
        /// </param>
        /// <param name="requires">
        /// The variables that this request handler requires to create a request.
        /// </param>
        /// <param name="forwardedVariables">
        /// The variables that this request handler forwards to the subgraph.
        /// </param>
        /// <param name="path">
        /// The path to the data that this request handler needs to extract.
        /// </param>
        /// <param name="transportFeatures">
        /// The transport features that are required by this node.
        /// </param>
        public Config(
            string subgraphName,
            DocumentNode document,
            ISelection? parent,
            ISelectionSet selectionSet,
            List<RootSelection> rootSelections,
            IEnumerable<string> provides,
            IEnumerable<string> requires,
            IEnumerable<string> forwardedVariables,
            IReadOnlyList<string> path,
            TransportFeatures transportFeatures)
        {
            // This is a temporary solution to selections being duplicated during request planning.
            // It should be properly fixed with new planner in the future.
            var rewriter = new SelectionRewriter();
            document = rewriter.RewriteDocument(document, null);

            string[]? buffer = null;
            var usedCapacity = 0;

            SubgraphName = subgraphName;
            Document = document.ToString(false);
            Parent = parent;
            RootSelections = rootSelections;
            SelectionSet = Unsafe.As<SelectionSet>(selectionSet);
            Provides = CollectionUtils.CopyToArray(provides, ref buffer, ref usedCapacity);
            Requires = CollectionUtils.CopyToArray(requires, ref buffer, ref usedCapacity);
            Path = CollectionUtils.CopyToArray(path);
            ForwardedVariables = CollectionUtils.CopyToArray(forwardedVariables, ref buffer, ref usedCapacity);
            TransportFeatures = transportFeatures;

            if (buffer is not null && usedCapacity > 0)
            {
                buffer.AsSpan().Slice(0, usedCapacity).Clear();
                ArrayPool<string>.Shared.Return(buffer);
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Gets the schema name on which this request handler executes.
        /// </summary>
        public string SubgraphName { get; }

        /// <summary>
        /// Gets the GraphQL request document.
        /// </summary>
        public string Document { get; }

        /// <summary>
        /// Gets the parent selection from which the selection set was composed.
        /// </summary>
        public ISelection? Parent { get; }

        /// <summary>
        /// Gets the root selections of this subgraph request.
        /// </summary>
        public List<RootSelection> RootSelections { get; }

        /// <summary>
        /// Gets the selection set for which this request provides a patch.
        /// </summary>
        public SelectionSet SelectionSet { get; }

        /// <summary>
        /// Gets the variables that this request handler requires to create a request.
        /// </summary>
        public string[] Provides { get; }

        /// <summary>
        /// Gets the variables that this request handler requires to create a request.
        /// </summary>
        public string[] Requires { get; }

        /// <summary>
        /// Gets the path to the data that this request handler needs to extract.
        /// </summary>
        public string[] Path { get; }

        /// <summary>
        /// Gets the variables that this request handler forwards to the subgraph.
        /// </summary>
        public string[] ForwardedVariables { get; }

        /// <summary>
        /// Gets the required transport features for this resolver node.
        /// </summary>
        public TransportFeatures TransportFeatures { get; }

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// The ResolverNodeBase.Config is not initialized.
        /// </exception>
        public void ThrowIfNotInitialized()
        {
            if (!_isInitialized)
            {
                throw new ArgumentNullException("The ResolverNodeBase.Config is not initialized.");
            }
        }
    }
}
