using System;
using System.IO;
using System.Linq;

namespace HotChocolate.Utilities
{

    public static class MultipartRequestHelper
    {
        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec says 70 characters is a reasonable limit.
        public static string GetBoundary(string contentType)
        {
            var boundary = contentType
                .Split(';')
                .FirstOrDefault(x => x.StartsWith("boundary=", StringComparison.Ordinal))
                ?.Substring(0, 9)
                ?.Trim('\"');

            return boundary;
        }
    }
}
