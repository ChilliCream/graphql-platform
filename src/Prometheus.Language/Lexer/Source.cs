using System;
using System.IO;

namespace Prometheus.Language
{
    public class Source
        : ISource
    {
        private readonly string _body;

        public Source(string body)
        {
            _body = body ?? string.Empty;
        }

        public bool IsEndOfStream(int position)
        {
            if (position >= _body.Length)
            {
                return true;
            }
            return false;
        }

        public char Read(int position)
        {
            if (position >= _body.Length)
            {
                // TODO: message
                throw new ArgumentNullException();
            }
            return _body[position];
        }

        public string Read(int startIndex, int length)
        {
            // TODO: exceptions
            return _body.Substring(startIndex, length);
        }
    }
}