using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    public delegate void OnSchemaError(
        IDescriptorContext descriptorContext,
        Exception exception);
}
