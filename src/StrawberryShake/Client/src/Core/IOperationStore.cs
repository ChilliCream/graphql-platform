using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace StrawberryShake
{
    /// <summary>
    /// The operation store tracks and stores results by requests.
    /// </summary>
    public interface IOperationStore
    {
        /// <summary>
        /// Stores the <paramref name="operationResult"/> for the specified
        /// <paramref name="operationRequest"/>.
        /// </summary>
        /// <param name="operationRequest">
        /// The operation request for which a result shall be stored.
        /// </param>
        /// <param name="operationResult">
        /// The operation result that shall be stored.
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/>.
        /// </param>
        /// <typeparam name="TResultData">
        /// The type of result data.
        /// </typeparam>
        void Set<TResultData>(
            OperationRequest operationRequest,
            IOperationResult<TResultData> operationResult)
            where TResultData : class;

        /// <summary>
        /// Tries to retrieve for a <paramref name="operationRequest"/>.
        /// </summary>
        /// <param name="operationRequest">
        /// The operation request for which a result shall be retrieved.
        /// </param>
        /// <param name="result">
        /// The retrieved operation result.
        /// </param>
        /// <typeparam name="TResultData">
        /// The type of result data.
        /// </typeparam>
        /// <returns>
        /// <c>true</c>, a result was found for the specified <paramref name="operationRequest"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        bool TryGet<TResultData>(
            OperationRequest operationRequest,
            [NotNullWhen(true)] out IOperationResult<TResultData>? result)
            where TResultData : class;

        /// <summary>
        /// Watches for updates to a <paramref name="operationRequest"/>.
        /// </summary>
        /// <param name="operationRequest">
        /// The operation request that is being observed.
        /// </param>
        /// <typeparam name="TResultData">
        /// The type of result data.
        /// </typeparam>
        /// <returns>
        /// Returns an operation observable which can be used to observe
        /// updates to the result of the specified <paramref name="operationRequest"/>.
        /// </returns>
        IObservable<IOperationResult<TResultData>> Watch<TResultData>(
            OperationRequest operationRequest)
            where TResultData : class;
    }
}
