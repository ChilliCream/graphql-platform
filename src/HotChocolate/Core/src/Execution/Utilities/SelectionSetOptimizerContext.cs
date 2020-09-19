using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    /// The selection set optimizer context 
    /// </summary>
    public readonly ref struct SelectionSetOptimizerContext
    {
        private readonly CompileResolverPipeline _compileResolverPipeline;

        public SelectionSetOptimizerContext(
            ISchema schema,
            Stack<IObjectField> fieldContext,
            IObjectType typeContext,
            SelectionSetNode selectionSet,
            IDictionary<string, Selection> fields,
            CompileResolverPipeline compileResolverPipeline)
        {
            Schema = schema;
            FieldContext = fieldContext;
            TypeContext = typeContext;
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
        public Stack<IObjectField> FieldContext { get; }

        /// <summary>
        /// Gets the type context of the current selection-set.
        /// </summary>
        public IObjectType TypeContext { get; }

        /// <summary>
        /// Gets the selection-set.
        /// </summary>
        public SelectionSetNode SelectionSet { get; }

        /// <summary>
        /// Gets the field set representing the compiled selection-set.
        /// </summary>
        /// <value></value>
        public IDictionary<string, Selection> Fields { get; }

        /// <summary>
        /// Allows to compile the field resolver pipeline for a field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="selection">The selection of the field.</param>
        /// <returns>
        /// Returns a <see cref="FieldDelegate" /> representing the field resolver pipeline.
        /// </returns>
        public FieldDelegate CompileResolverPipeline(
            IObjectField field,
            FieldNode selection) =>
            _compileResolverPipeline(field, selection);
    }
}
