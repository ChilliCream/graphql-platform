using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

/// <summary>
/// Represents a callback that is invoked when an error occurs during schema creation.
/// </summary>
public delegate void OnSchemaError(
    IDescriptorContext descriptorContext,
    Exception exception);
