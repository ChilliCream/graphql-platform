using System;
using HotChocolate.Execution;

namespace HotChocolate.Lodash
{
    public static class ThrowHelper
    {
        public static Exception ExpectArrayButReceivedObject(string path) =>
            JsonAggregationException.Create(
                "AG0001",
                "The field {0} expects a array but received an object",
                path);

        public static Exception ExpectArrayButReceivedScalar(string path) =>
            JsonAggregationException.Create(
                "AG0004",
                "The field {0} expects a array but received an scalar",
                path);

        public static Exception ExpectObjectButReceivedScalar(string path) =>
            JsonAggregationException.Create(
                "AG0002",
                "The field {0} expects a object but received an scalar",
                path);

        public static Exception ExpectObjectButReceivedArray(string path) =>
            JsonAggregationException.Create(
                "AG0003",
                "The field {0} expects a object but received an array",
                path);
    }
}
