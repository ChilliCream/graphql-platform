using System;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputFieldDescriptor
        : IInputFieldDescriptor
    {
        public InputFieldDescriptor(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Property = property;
            Name = property.GetGraphQLName();
        }

        public PropertyInfo Property { get; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public Type NativeType { get; protected set; }

        public IValueNode DefaultValue { get; protected set; }

        public object NativeDefaultValue { get; protected set; }


        #region IInputFieldDescriptor

        IInputFieldDescriptor IInputFieldDescriptor.Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The input field name cannot be null or empty.",
                    nameof(name));
            }

            if (ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL input field name.",
                    nameof(name));
            }

            Name = name;
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Description(string description)
        {
            throw new System.NotImplementedException();
        }

        IInputFieldDescriptor IInputFieldDescriptor.Type<TInputType>()
        {
            throw new System.NotImplementedException();
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(IValueNode defaultValue)
        {
            throw new System.NotImplementedException();
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(object defaultValue)
        {
            throw new System.NotImplementedException();
        }





        #endregion
    }
}
