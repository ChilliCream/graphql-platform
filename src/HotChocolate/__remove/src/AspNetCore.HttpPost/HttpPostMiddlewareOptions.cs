using System;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public class HttpPostMiddlewareOptions
        : IHttpPostMiddlewareOptions
    {
        private const int _minMaxRequestSize = 1024;
        private PathString _path = new PathString("/");
        private ParserOptions _parserOptions = new ParserOptions();
        private int _maxRequestSize = 20 * 1000 * 1000;

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

        public ParserOptions ParserOptions
        {
            get => _parserOptions;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _parserOptions = value;
            }
        }

        public int MaxRequestSize
        {
            get => _maxRequestSize;
            set
            {
                if (value < _minMaxRequestSize)
                {
                    // TODO : resources
                    throw new ArgumentException(
                        "The minimum max request size is 1024 byte.",
                        nameof(value));
                }

                _maxRequestSize = value;
            }
        }
    }
}
