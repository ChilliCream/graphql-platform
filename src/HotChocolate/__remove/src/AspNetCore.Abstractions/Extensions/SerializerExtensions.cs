using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore
{
    public static class SerializerExtensions
    {
        public static Task SerializeAsync(
            this IQueryResultSerializer serializer,
            IExecutionResult result,
            Stream outputStream,
            CancellationToken cancellationToken)
        {
            if (result is IReadOnlyQueryResult queryResult)
            {
                using (queryResult)
                {
                    return serializer.SerializeAsync(
                        queryResult,
                        outputStream,
                        cancellationToken);
                }
            }
            else
            {
                // TODO : resources
                return serializer.SerializeAsync(
                    QueryResultBuilder.CreateError(
                        ErrorBuilder.New()
                            .SetMessage("Result type not supported.")
                            .SetCode(ErrorCodes.Serialization.ResultTypeNotSupported)
                            .Build()),
                    outputStream,
                    cancellationToken);
            }
        }
    }
}
