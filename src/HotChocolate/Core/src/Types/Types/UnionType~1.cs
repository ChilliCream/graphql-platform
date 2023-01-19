using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

public class UnionType<T>
    : UnionType
{
    private Action<IUnionTypeDescriptor>? _configure;

    public UnionType()
    {
        _configure = Configure;
    }

    public UnionType(Action<IUnionTypeDescriptor> configure)
    {
        _configure = configure
            ?? throw new ArgumentNullException(nameof(configure));
    }

    protected override UnionTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var descriptor =
            UnionTypeDescriptor.New(context.DescriptorContext, typeof(T));

        _configure!(descriptor);
        _configure = null;

        return descriptor.CreateDefinition();
    }
}
