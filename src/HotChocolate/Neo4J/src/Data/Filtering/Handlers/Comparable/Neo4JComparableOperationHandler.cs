using HotChocolate.Configuration;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// The base of a Neo4j operation handler specific for
    /// <see cref="IComparableOperationFilterInputType "/>
    /// If the <see cref="FilterTypeInterceptor"/> encounters a operation field that implements
    /// <see cref="IComparableOperationFilterInputType "/> and matches the operation identifier
    /// defined in <see cref="Neo4JComparableOperationHandler.Operation"/> the handler is bound to
    /// the field
    /// </summary>
    public abstract class Neo4JComparableOperationHandler
        : Neo4JFilterOperationHandlerBase
    {
        /// <summary>
        /// Specifies the identifier of the operations that should be handled by this handler
        /// </summary>
        protected abstract int Operation { get; }

        /// <summary>
        /// Checks if the <see cref="FilterField"/> implements
        /// <see cref="IComparableOperationFilterInputType "/> and has the operation identifier
        /// defined in <see cref="Neo4JComparableOperationHandler.Operation"/>
        /// </summary>
        /// <param name="context">The discovery context of the schema</param>
        /// <param name="typeDefinition">The definition of the declaring type of the field</param>
        /// <param name="fieldDefinition">The definition of the field</param>
        /// <returns>Returns true if the field can be handled</returns>
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IComparableOperationFilterInputType &&
                   fieldDefinition is FilterOperationFieldDefinition operationField &&
                   operationField.Id == Operation;
        }
    }
}
