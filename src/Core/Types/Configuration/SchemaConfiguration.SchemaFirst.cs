using System.Collections.Generic;
using System;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
    {
        public IBindResolverDelegate BindResolver(
            FieldResolverDelegate fieldResolver)
        {
            return new BindResolverDelegate(bindingInfo);
        }

        public IBindResolver<TResolver> BindResolver<TResolver>(
            BindingBehavior bindingBehavior)
            where TResolver : class
        {

        }

        public IBindType<T> BindType<T>(
            BindingBehavior bindingBehavior)
            where T : class
        {

        }

        public void RegisterIsOfType(IsOfTypeFallback isOfType)
        {
        }
    }
}
