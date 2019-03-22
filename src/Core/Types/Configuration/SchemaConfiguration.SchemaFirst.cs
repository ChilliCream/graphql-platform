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
        private List<IBindingBuilder> _bindingBuilders =
            new List<IBindingBuilder>();

        public IBindResolverDelegate BindResolver(
            FieldResolverDelegate fieldResolver)
        {
            IResolverBindingBuilder builder =
                ResolverBindingBuilder.New()
                    .SetResolver(fieldResolver);
            _bindingBuilders.Add(builder);
            return new BindResolverDelegate(builder);
        }

        public IBindResolver<TResolver> BindResolver<TResolver>(
            BindingBehavior bindingBehavior)
            where TResolver : class
        {
            IResolverTypeBindingBuilder builder =
                ResolverTypeBindingBuilder.New()
                    .SetResolverType(typeof(TResolver));
            _bindingBuilders.Add(builder);
            return new BindResolver<TResolver>(builder);
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
