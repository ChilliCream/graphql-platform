using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration
{
    internal interface ITypeRegistrar
    {
        void Register(TypeSystemObjectBase typeSystemObject, bool isInferred = false);

        void MarkUnresolved(ITypeReference typeReference);

        bool IsResolved(IClrTypeReference typeReference);

        TypeSystemObjectBase CreateInstance(Type namedSchemaType);

        IReadOnlyCollection<ITypeReference> GetUnresolved();
    }
}
