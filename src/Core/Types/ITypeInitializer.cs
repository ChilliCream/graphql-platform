using System;

namespace HotChocolate.Types
{
    internal interface ITypeInitializer
    {
        void CompleteInitialization(
            Action<SchemaError> reportError);
    }
}
