using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Grpc
{
    internal static class ExecutionResultExtensions
    {
        /// <summary>
        /// Mapper for mapping <see cref="IExecutionResult"/> to <see cref="QueryResponse"/>
        /// </summary>
        /// <param name="result"><see cref="IExecutionResult"/></param>
        /// <returns><see cref="QueryResponse"/></returns>
        public static QueryResponse ToGrpcQueryResponse(this IExecutionResult result)
        {
            var queryResult = (IReadOnlyQueryResult)result;
            var response = new QueryResponse
            {
                Data = queryResult.Data.ToStruct(),
                Errors = { result.Errors.ToGrpcErrors() },
                Extensions = queryResult.Extensions.ToStruct(),
                // TODO: Return correct Path
                Path = new ListValue
                {
                    Values =
                    {
                        Value.ForNull()
                    }
                }
            };

            return response;
        }

        /// <summary>
        /// Mapper for mapping collection of <see cref="IError" /> to collection of <see cref="Error"/>
        /// </summary>
        /// <param name="errors"><see cref="IEnumerable&lt;IError&gt;"/></param>
        /// <returns><see cref="IEnumerable&lt;Error&gt;"/></returns>
        private static IEnumerable<Error> ToGrpcErrors(this IEnumerable<IError> errors)
        {
            var resultErrors = new RepeatedField<Error>();
            resultErrors.AddRange(errors.Select(e =>
            {
                var locations = new RepeatedField<SourceLocation>();
                locations.AddRange(e.Locations.Select(l =>
                    new SourceLocation
                    {
                        Column = l.Column,
                        Line = l.Line
                    }));

                return new Error
                {
                    Message = e.Message,
                    Locations = { locations }
                };
            }));

            return resultErrors;
        }
    }
}
