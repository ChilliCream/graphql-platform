using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.AspNetCore
{
    internal class HttpMultipartRequest
    {
        public HttpMultipartRequest(string operations, IDictionary<string, IFile> fileMap)
        {
            Operations = operations;
            FileMap = fileMap;
        }

        public string Operations { get; set; }

        public IDictionary<string, IFile> FileMap { get; set; }
    }
}
