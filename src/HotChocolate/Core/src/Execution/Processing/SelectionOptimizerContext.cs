using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// The selection optimizer context
    /// </summary>
    public readonly ref struct SelectionOptimizerContext
    {
        private readonly CompileResolverPipeline _compileResolverPipeline;

        /// <summary>
        /// Initializes a new instance of <see cref="SelectionOptimizerContext"/>
        /// </summary>
        /// <param name="schema">
        /// The schema.
        /// </param>
        /// <param name="path">
        /// The field path.
        /// </param>
        /// <param name="type">
        /// The declaring type of the fields.
        /// </param>
        /// <param name="selectionSet">
        /// The selection set that is currently being compiled.
        /// </param>
        /// <param name="fields">
        /// The fields.
        /// </param>
        /// <param name="compileResolverPipeline">
        /// A helper to compile the resolver pipeline of a field.
        /// </param>
        public SelectionOptimizerContext(
            ISchema schema,
            IImmutableStack<IObjectField> path,
            IObjectType type,
            SelectionSetNode selectionSet,
            IDictionary<string, ISelection> fields,
            CompileResolverPipeline compileResolverPipeline)
        {
            Schema = schema;
            Path = path;
            Type = type;
            SelectionSet = selectionSet;
            Fields = fields;
            _compileResolverPipeline = compileResolverPipeline;
        }

        /// <summary>
        /// Gets the schema for which the query is compiled.
        /// </summary>
        public ISchema Schema { get; }

        /// <summary>
        /// Gets the field execution stack.
        /// </summary>
        public IImmutableStack<IObjectField> Path { get; }

        /// <summary>
        /// Gets the type context of the current selection-set.
        /// </summary>
        public IObjectType Type { get; }

        /// <summary>
        /// Gets the selection-set.
        /// </summary>
        public SelectionSetNode SelectionSet { get; }

        /// <summary>
        /// Gets the field set representing the compiled selection-set.
        /// </summary>
        /// <value></value>
        public IDictionary<string, ISelection> Fields { get; }

        /// <summary>
        /// Allows to compile the field resolver pipeline for a field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="selection">The selection of the field.</param>
        /// <returns>
        /// Returns a <see cref="FieldDelegate" /> representing the field resolver pipeline.
        /// </returns>
        public FieldDelegate CompileResolverPipeline(IObjectField field, FieldNode selection) =>
            _compileResolverPipeline(field, selection);
    }
}
