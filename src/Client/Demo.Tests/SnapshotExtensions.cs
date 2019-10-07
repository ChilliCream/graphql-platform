using System.Collections.Generic;
using Snapshooter.Xunit;

namespace StrawberryShake.Demo
{
    public static class SnapshotExtensions
    {
        public static void MatchSnapshot(this IOperationResult result)
        {
            var dict = new Dictionary<string, object?>();
            dict[nameof(result.Data)] = result.Data;
            dict[nameof(result.Errors)] = result.Errors;
            dict[nameof(result.Extensions)] = result.Extensions;
            dict.MatchSnapshot();
        }
    }
}
