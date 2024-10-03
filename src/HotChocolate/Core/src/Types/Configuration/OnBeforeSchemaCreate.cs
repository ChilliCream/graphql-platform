using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

/// <summary>
/// Represents a callback that is invoked before a schema is created.
/// </summary>
public delegate void OnBeforeSchemaCreate(
    IDescriptorContext descriptorContext,
    ISchemaBuilder schemaBuilder);
