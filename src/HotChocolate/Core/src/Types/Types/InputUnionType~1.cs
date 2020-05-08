using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class InputUnionType<T>
        : InputUnionType
    {
        private readonly Action<IInputUnionTypeDescriptor> _configure;

        public InputUnionType()
        {
            _configure = Configure;
        }

        public InputUnionType(Action<IInputUnionTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        protected override InputUnionTypeDefinition CreateDefinition(IInitializationContext context)
        {
            var descriptor = InputUnionTypeDescriptor.New(
                context.DescriptorContext,
                typeof(T));
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected override void OnCompleteTypeSet(
            ICompletionContext context,
            InputUnionTypeDefinition definition,
            ISet<InputObjectType> typeSet)
        {
            base.OnCompleteTypeSet(context, definition, typeSet);

            Type markerType = definition.ClrType;

            if (markerType != typeof(object))
            {
                foreach (InputObjectType type in context.GetTypes<InputObjectType>())
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
