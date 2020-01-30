using System;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public class HttpGetSchemaMiddlewareOptions
        : IHttpGetSchemaMiddlewareOptions
    {
        private PathString _path = new PathString("/");

        public PathString Path
        {
            get => _path;
            set
            {
                if (!value.HasValue)
                {
                    // TODO : resources
                    throw new ArgumentException(
                        "The path cannot be empty.");
                }

                _path = value;
            }
        }
    }
}
