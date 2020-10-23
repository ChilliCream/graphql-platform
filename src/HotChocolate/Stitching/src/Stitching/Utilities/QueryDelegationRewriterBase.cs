using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class QueryDelegationRewriterBase
        : IQueryDelegationRewriter
    {
        /// <summary>
        /// This method will be called after the stitching layer
        /// has rewritten a field and allows to add custom rewriter logic.
        /// </summary>
        /// <param name="targetSchemaName">
        /// The name of the schema to which the query shall be delegated.
        /// </param>
        /// <param name="outputType">
        /// The current output type on which the selection set is declared.
        /// </param>
        /// <param name="outputField">
        /// The current output field on which this selection set is declared.
        /// </param>
        /// <param name="selectionSet">
        /// The list of selections that shall be added to the delegation query.
        /// </param>
        public virtual FieldNode OnRewriteField(
            NameString targetSchemaName,
            IOutputType outputType,
            IOutputField outputField,
            FieldNode field)
        {
            return field;
        }

        /// <summary>
        /// This method will be called after the stitching layer
        /// has rewritten a selection set and allows to add custom
        /// rewriter logic.
        /// </summary>
        /// <param name="targetSchemaName">
        /// The name of the schema to which the query shall be delegated.
        /// </param>
        /// <param name="outputType">
        /// The current output type on which the selection set is declared.
        /// </param>
        /// <param name="outputField">
        /// The current output field on which this selection set is declared.
        /// </param>
        /// <param name="selectionSet">
        /// The list of selections that shall be added to the delegation query.
        /// </param>
        public virtual SelectionSetNode OnRewriteSelectionSet(
            NameString targetSchemaName,
            IOutputType outputType,
            IOutputField outputField,
            SelectionSetNode selectionSet)
        {
            return selectionSet;
        }
    }
}
