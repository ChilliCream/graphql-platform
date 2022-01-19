using System;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

public class Representation
{
    public Representation(NameString typeName, ObjectValueNode data)
    {
        TypeName = typeName.EnsureNotEmpty(nameof(typeName));
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public NameString TypeName { get; }

    public ObjectValueNode Data { get; }
}
