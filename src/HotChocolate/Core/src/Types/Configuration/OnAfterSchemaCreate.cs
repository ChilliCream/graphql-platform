using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

/// <summary>
/// Represents a callback that is invoked after a schema has been created.
/// </summary>
public delegate void OnAfterSchemaCreate(
    IDescriptorContext descriptorContext,
    ISchema schema);
