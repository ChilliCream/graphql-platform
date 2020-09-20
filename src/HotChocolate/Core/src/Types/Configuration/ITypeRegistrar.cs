using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration
{
    internal interface ITypeRegistrar
    {
        void Register(
            TypeSystemObjectBase typeSystemObject, 
            string? scope,
            bool isInferred = false);

        void MarkUnresolved(ITypeReference typeReference);

        void MarkResolved(ITypeReference typeReference);

        bool IsResolved(ITypeReference typeReference);

        TypeSystemObjectBase CreateInstance(Type namedSchemaType);

        IReadOnlyCollection<ITypeReference> GetUnresolved();

        IReadOnlyCollection<ITypeReference> GetUnhandled();
    }
}
