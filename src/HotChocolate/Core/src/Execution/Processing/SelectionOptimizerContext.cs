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
        private readonly OperationCompiler _compiler;
        private readonly OperationCompiler.CompilerContext _compilerContext;

        /// <summary>
        /// Initializes a new instance of <see cref="SelectionOptimizerContext"/>
        /// </summary>
        internal SelectionOptimizerContext(
            OperationCompiler compiler,
            OperationCompiler.CompilerContext compilerContext)
        {
            _compiler = compiler;
            _compilerContext = compilerContext;
        }

        /// <summary>
        /// Gets the schema for which the query is compiled.
        /// </summary>
        public ISchema Schema => _compiler.Schema;

        /// <summary>
        /// Gets the field execution stack.
        /// </summary>
        public IImmutableStack<IObjectField> Path => _compilerContext.Path;

        /// <summary>
        /// Gets the type context of the current selection-set.
        /// </summary>
        public IObjectType Type => _compilerContext.Type;

        /// <summary>
        /// Gets the selection-set.
        /// </summary>
        public SelectionSetNode SelectionSet => _compilerContext.SelectionSet;

        /// <summary>
        /// Gets the field set representing the compiled selection-set.
        /// </summary>
        /// <value></value>
        public IDictionary<string, Selection> Fields => _compilerContext.Fields;

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
            _compiler.CreateFieldMiddleware(field, selection);

        /// <summary>
        /// Gets the next operation unique selection id.
        /// </summary>
        public int GetNextId() => _compiler.GetNextId();
    }
}
