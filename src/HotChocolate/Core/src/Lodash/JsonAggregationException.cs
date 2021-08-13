using System;

namespace HotChocolate.Lodash
{
    public class JsonAggregationException : Exception
    {
        public JsonAggregationException(
            string code,
            string message)
            : base(message)
        {
            Code = code;
        }

        public string Code { get; set; }

        public static JsonAggregationException Create(
            string code,
            string message,
            params string[] elements) =>
            new(code, string.Format(message, elements));
    }
}
