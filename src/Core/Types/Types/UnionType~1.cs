using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class UnionType<T>
        : UnionType
    {
        private readonly Action<IUnionTypeDescriptor> _configure;

        public UnionType()
        {
            _configure = Configure;
        }

        public UnionType(Action<IUnionTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        protected override UnionTypeDefinition CreateDefinition(IInitializationContext context)
        {
            var descriptor = UnionTypeDescriptor.New(
                context.DescriptorContext,
                typeof(T));
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected override void OnCompleteTypeSet(
            ICompletionContext context,
            UnionTypeDefinition definition,
            ISet<ObjectType> typeSet)
        {
            base.OnCompleteTypeSet(context, definition, typeSet);

            Type markerType = definition.ClrType;

            if (markerType != typeof(object))
            {
                foreach (ObjectType type in context.GetTypes<ObjectType>())
                {
                    if (type.ClrType != typeof(object)
                        && markerType.IsAssignableFrom(type.ClrType))
                    {
                        typeSet.Add(type);
                    }
                }
            }
        }
    }
}
