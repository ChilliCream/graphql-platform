using System;
using HotChocolate.Execution;

namespace HotChocolate.Lodash
{
    public static class ThrowHelper
    {
        /// <summary>
        /// ErrorCode: AG0001
        /// </summary>
        public static Exception ExpectArrayButReceivedObject(string path) =>
            JsonAggregationException.Create(
                "AG0001",
                "The field {0} expects a list but received an object",
                path);

        /// <summary>
        /// ErrorCode: AG0004
        /// </summary>
        public static Exception ExpectArrayButReceivedScalar(string path) =>
            JsonAggregationException.Create(
                "AG0004",
                "The field {0} expects a list but received a scalar",
                path);

        /// <summary>
        /// ErrorCode: AG0002
        /// </summary>
        public static Exception ExpectObjectButReceivedScalar(string path) =>
            JsonAggregationException.Create(
                "AG0002",
                "The field {0} expects a object but received a scalar",
                path);

        /// <summary>
        /// ErrorCode: AG0003
        /// </summary>
        public static Exception ExpectObjectButReceivedArray(string path) =>
            JsonAggregationException.Create(
                "AG0003",
                "The field {0} expects a object but received an list",
                path);

        /// <summary>
        /// ErrorCode: AG0005
        /// </summary>
        public static Exception ChunkCountCannotBeLowerThanOne(string path) =>
            JsonAggregationException.Create(
                "AG0005",
                "The argument size of chunk on field {0} must be greater than 0",
                path);

        /// <summary>
        /// ErrorCode: AG0006
        /// </summary>
        public static Exception FlattenDepthCannotBeLowerThanOne(string path) =>
            JsonAggregationException.Create(
                "AG0006",
                "The argument size of flatten on field {0} must be greater than 0",
                path);
    }
}
