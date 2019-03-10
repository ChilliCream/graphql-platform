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
                .Select(x => x.Trim())
                .FirstOrDefault(x => x.StartsWith("boundary=", StringComparison.Ordinal))
                ?.Substring(9)
                ?.Trim('\"');

            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            return boundary;
        }
    }
}
