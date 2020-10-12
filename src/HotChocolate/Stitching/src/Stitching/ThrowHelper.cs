using System;
using HotChocolate.Language;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching
{
    internal static class ThrowHelper
    {
        public static InvalidOperationException BufferedRequest_VariableDoesNotExist(
            string name) =>
            new InvalidOperationException(string.Format(
                ThrowHelper_BufferedRequest_VariableDoesNotExist,
                name));

        public static InvalidOperationException BufferedRequest_OperationNotFound(
            DocumentNode document) =>
            new InvalidOperationException(string.Format(
                ThrowHelper_BufferedRequest_OperationNotFound,
                document));
    }
}
