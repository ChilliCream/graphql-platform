using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public class HttpMultipartRequest
    {
        public HttpMultipartRequest(string operations, IDictionary<string, IFormFile> fileMap)
        {
            Operations = operations;
            FileMap = fileMap;
        }

        public string Operations { get; set; }

        public IDictionary<string, IFormFile> FileMap { get; set; }
    }
}