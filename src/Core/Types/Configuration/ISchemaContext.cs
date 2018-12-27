﻿using System;
using System.Collections.Generic;
using HotChocolate.Runtime;

namespace HotChocolate.Configuration
{
    internal interface ISchemaContext
    {
        IServiceProvider Services { get; }
        ITypeRegistry Types { get; }
        IDirectiveRegistry Directives { get; }
        IResolverRegistry Resolvers { get; }
        ICollection<DataLoaderDescriptor> DataLoaders { get; }
        ICollection<CustomContextDescriptor> CustomContexts { get; }
    }
}
