using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class ObjectFieldDescriptorAttribute : Attribute
    {
        public abstract void OnConfigure(IObjectFieldDescriptor descriptor);
    }

    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class InputFieldDescriptorAttribute : Attribute
    {
        public abstract void OnConfigure(IInputFieldDescriptor descriptor);
    }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class ObjectTypeDescriptorAttribute : Attribute
    {
        public abstract void OnConfigure(IObjectTypeDescriptor descriptor);
    }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = true)]
    public abstract class InputObjectTypeDescriptorAttribute : Attribute
    {
        public abstract void OnConfigure(IInputObjectTypeDescriptor descriptor);
    }
}
