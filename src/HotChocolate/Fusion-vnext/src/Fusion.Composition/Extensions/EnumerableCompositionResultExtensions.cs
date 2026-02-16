using System.Collections.Immutable;
using HotChocolate.Fusion.Results;

namespace HotChocolate.Fusion.Extensions;

internal static class EnumerableCompositionResultExtensions
{
    extension(IEnumerable<CompositionResult> results)
    {
        public CompositionResult Combine()
        {
            var failedResults = results.Where(r => r.IsFailure).ToImmutableArray();

            if (failedResults.Length == 0)
            {
                return CompositionResult.Success();
            }

            return failedResults.SelectMany(r => r.Errors).ToImmutableArray();
        }
    }

    extension<T>(IEnumerable<CompositionResult<T>> results)
    {
        public CompositionResult<IEnumerable<T>> Combine()
        {
            var resultsArray = results.ToImmutableArray();
            var failedResults = resultsArray.Where(r => r.IsFailure).ToImmutableArray();

            return failedResults.Length == 0
                ? resultsArray.Select(r => r.Value).ToImmutableArray()
                : failedResults.SelectMany(r => r.Errors).ToImmutableArray();
        }
    }
}
