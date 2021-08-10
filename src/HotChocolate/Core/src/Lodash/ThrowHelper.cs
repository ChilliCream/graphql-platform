using System;
using HotChocolate.Execution;

namespace HotChocolate.Lodash
{
    public static class ThrowHelper
    {
        public static Exception ExpectArrayButReceivedObject(string path) =>
            new QueryException(
                ErrorBuilder
                    .New()
                    .SetMessage("The field {0} expects a array but received an object", path)
                    .Build());

        public static Exception ExpectObjectButReceivedScalar(string path) =>
            new QueryException(
                ErrorBuilder
                    .New()
                    .SetMessage("The field {0} expects a object but received an scalar", path)
                    .Build());

        public static Exception ExpectObjectButReceivedArray(string path) =>
            new QueryException(
                ErrorBuilder
                    .New()
                    .SetMessage("The field {0} expects a object but received an array", path)
                    .Build());
    }
}
