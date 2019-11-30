using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class ObjectFieldDescriptorAttribute
        : DescriptorAttribute
    {
        internal sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IObjectFieldDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IObjectFieldDescriptor descriptor);
    }

    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class ArgumentDescriptorAttribute
        : DescriptorAttribute
    {
        internal sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IArgumentDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IArgumentDescriptor descriptor);
    }

    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class InputFieldDescriptorAttribute
        : DescriptorAttribute
    {
        internal sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IInputFieldDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IInputFieldDescriptor descriptor);
    }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class ObjectTypeDescriptorAttribute
        : DescriptorAttribute
    {
        internal sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IObjectTypeDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IObjectTypeDescriptor descriptor);
    }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class InputObjectTypeDescriptorAttribute
        : DescriptorAttribute
    {
        internal sealed override void TryConfigure(IDescriptor descriptor)
        {
            if (descriptor is IInputObjectTypeDescriptor d)
            {
                OnConfigure(d);
            }
        }

        public abstract void OnConfigure(IInputObjectTypeDescriptor descriptor);
    }

}
