using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb.Filters;

/// <summary>
/// The base of a mongodb operation handler specific for
/// <see cref="IComparableOperationFilterInputType "/>
/// If the <see cref="FilterTypeInterceptor"/> encounters an operation field that implements
/// <see cref="IComparableOperationFilterInputType "/> and matches the operation identifier
/// defined in <see cref="MongoDbComparableOperationHandler.Operation"/> the handler is bound to
/// the field
/// </summary>
public abstract class MongoDbComparableOperationHandler
    : MongoDbOperationHandlerBase
{
    public MongoDbComparableOperationHandler(InputParser inputParser) : base(inputParser)
    {
    }

    /// <summary>
    /// Specifies the identifier of the operations that should be handled by this handler
    /// </summary>
    protected abstract int Operation { get; }

    /// <summary>
    /// Checks if the <see cref="FilterField"/> implements
    /// <see cref="IComparableOperationFilterInputType "/> and has the operation identifier
    /// defined in <see cref="MongoDbComparableOperationHandler.Operation"/>
    /// </summary>
    /// <param name="context">The discovery context of the schema</param>
    /// <param name="typeConfiguration">The configuration of the declaring type of the field</param>
    /// <param name="fieldConfiguration">The configuration of the field</param>
    /// <returns>Returns true if the field can be handled</returns>
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeConfiguration,
        IFilterFieldConfiguration fieldConfiguration)
    {
        return context.Type is IComparableOperationFilterInputType &&
            fieldConfiguration is FilterOperationFieldConfiguration operationField &&
            operationField.Id == Operation;
    }
}
