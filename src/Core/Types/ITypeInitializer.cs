using System;

namespace HotChocolate.Types
{
    internal interface ITypeInitializer
    {
        void CompleteInitialization(
            SchemaContext schemaContext,
            Action<SchemaError> reportError);
    }
}
