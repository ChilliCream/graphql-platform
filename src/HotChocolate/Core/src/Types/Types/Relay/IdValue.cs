#nullable enable

using System;

namespace HotChocolate.Types.Relay;

public readonly struct IdValue
{
    public IdValue(string? schemaName, string typeName, object value)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        SchemaName = schemaName;
        TypeName = typeName;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string? SchemaName { get; }

    public string TypeName { get; }

    public object Value { get; }
}
