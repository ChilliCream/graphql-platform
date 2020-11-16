using System.Collections.Generic;
using System;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class SchemaConfiguration
    {
        private List<IBindingBuilder> _bindingBuilders =
            new List<IBindingBuilder>();

        public IBindResolverDelegate BindResolver(
            FieldResolverDelegate fieldResolver)
        {
            if (fieldResolver is null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

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
                    .SetFieldBinding(bindingBehavior)
                    .SetResolverType(typeof(TResolver));
            _bindingBuilders.Add(builder);
            return new BindResolver<TResolver>(builder);
        }

        public IBindType<T> BindType<T>(
            BindingBehavior bindingBehavior)
            where T : class
        {
            IComplexTypeBindingBuilder builder =
                ComplexTypeBindingBuilder.New()
                    .SetFieldBinding(bindingBehavior)
                    .SetType(typeof(T));
            _bindingBuilders.Add(builder);
            return new BindType<T>(builder);
        }

        public ISchemaConfiguration RegisterIsOfType(IsOfTypeFallback isOfType)
        {
            _builder.SetTypeResolver(isOfType);
            return this;
        }
    }
}
