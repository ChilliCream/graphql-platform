using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InterfaceField
        : ObjectFieldBase
    {
        internal InterfaceField(Action<IInterfaceFieldDescriptor> configure)
            : this(() => ExecuteConfigure(configure))
        {
        }

        internal InterfaceField(
            Func<InterfaceFieldDescription> descriptionFactory)
            : this(DescriptorHelpers.ExecuteFactory(descriptionFactory))
        {
        }

        internal InterfaceField(InterfaceFieldDescription fieldDescription)
            : base(fieldDescription)
        {
        }

        private static InterfaceFieldDescription ExecuteConfigure(
            Action<IInterfaceFieldDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var descriptor = new InterfaceFieldDescriptor();
            configure(descriptor);
            return descriptor.CreateDescription();
        }
    }
}
