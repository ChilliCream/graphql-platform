using System;

#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class Named : INamed
    {
        public abstract SymbolicName? GetSymbolicName();
        public SymbolicName GetRequiredSymbolicName() => GetSymbolicName() ??
            throw new InvalidOperationException("No name present.");
    }
}
