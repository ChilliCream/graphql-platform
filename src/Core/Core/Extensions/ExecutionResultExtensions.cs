using System;
using HotChocolate.Execution;
using HotChocolate.Properties;
using Newtonsoft.Json;

namespace HotChocolate
{
    public static class ExecutionResultExtensions
    {
        private static readonly JsonSerializerSettings _settings =
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

        public static string ToJson(
            this IExecutionResult result) =>
            ToJson(result, true);

        public static string ToJson(
            this IExecutionResult result,
            bool withIndentations)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result is IReadOnlyQueryResult queryResult)
            {
                if (withIndentations)
                {
                    return JsonConvert.SerializeObject(
                        queryResult.ToDictionary(),
                        _settings);
                }
                return JsonConvert.SerializeObject(
                    queryResult.ToDictionary());
            }

            throw new NotSupportedException(
                CoreResources.ToJson_OnlyQueryResultsSupported);
        }
    }
}
