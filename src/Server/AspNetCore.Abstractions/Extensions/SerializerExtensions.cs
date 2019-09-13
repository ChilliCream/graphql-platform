using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;
#else
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
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
                return serializer.SerializeAsync(
                    queryResult,
                    outputStream,
                    cancellationToken);
            }
            else
            {
                // TODO : resources
                return serializer.SerializeAsync(
                    QueryResult.CreateError(
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
