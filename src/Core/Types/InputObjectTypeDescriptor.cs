using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;

namespace HotChocolate.Types
{
    internal class InputObjectTypeDescriptor
        : IInputObjectTypeDescriptor
    {
        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public Type NativeType { get; protected set; }

        protected ImmutableList<InputFieldDescriptor> Fields { get; set; } =
            ImmutableList<InputFieldDescriptor>.Empty;

        public virtual IReadOnlyCollection<InputFieldDescriptor> GetFieldDescriptors()
        {
            Dictionary<string, InputFieldDescriptor> descriptors =
                new Dictionary<string, InputFieldDescriptor>();
            foreach (InputFieldDescriptor descriptor in Fields)
            {
                descriptors[descriptor.Name] = descriptor;
            }
            return descriptors.Values;
        }

        #region IInputObjectTypeDescriptor

        IInputObjectTypeDescriptor IInputObjectTypeDescriptor.Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsTypeNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL type name.",
                    nameof(name));
            }

            Name = name;
            return this;
        }

        IInputObjectTypeDescriptor IInputObjectTypeDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        #endregion
    }

    internal class InputObjectTypeDescriptor<T>
        : InputObjectTypeDescriptor
        , IInputObjectTypeDescriptor<T>
    {
        public InputObjectTypeDescriptor(Type pocoType)
        {
            if (pocoType == null)
            {
                throw new ArgumentNullException(nameof(pocoType));
            }

            NativeType = pocoType;
            Name = pocoType.GetGraphQLName();
        }

        private BindingBehavior _bindingBehavior = BindingBehavior.Implicit;

        public override IReadOnlyCollection<InputFieldDescriptor> GetFieldDescriptors()
        {
            Dictionary<string, InputFieldDescriptor> descriptors =
                new Dictionary<string, InputFieldDescriptor>();

            foreach (InputFieldDescriptor descriptor in Fields)
            {
                descriptors[descriptor.Name] = descriptor;
            }

            if (_bindingBehavior == BindingBehavior.Implicit)
            {
                Dictionary<PropertyInfo, string> properties = GetProperties(NativeType);
                foreach (InputFieldDescriptor descriptor in descriptors.Values
                    .Where(t => t.Property != null))
                {
                    properties.Remove(descriptor.Property);
                }

                foreach (KeyValuePair<PropertyInfo, string> property in properties)
                {
                    if (!descriptors.ContainsKey(property.Value)
                        && TryCreateFieldDescriptorFromProperty(
                            property.Value, property.Key,
                            out InputFieldDescriptor descriptor))
                    {
                        descriptors[descriptor.Name] = descriptor;
                    }
                }
            }

            return descriptors.Values;
        }

        private bool TryCreateFieldDescriptorFromProperty(
            string name, PropertyInfo property,
            out InputFieldDescriptor descriptor)
        {
            descriptor = null;
            Type type = property.PropertyType;
            if (type != null && TypeInspector.Default.IsSupported(type))
            {
                descriptor = new InputFieldDescriptor(property);
            }
            return descriptor != null;
        }

        private static Dictionary<PropertyInfo, string> GetProperties(Type type)
        {
            Dictionary<PropertyInfo, string> properties =
                new Dictionary<PropertyInfo, string>();

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(t => t.DeclaringType != typeof(object)))
            {
                properties[property] = property.GetGraphQLName();
            }

            return properties;
        }

        #region IInputObjectTypeDescriptor<T>

        IInputObjectTypeDescriptor<T> IInputObjectTypeDescriptor<T>.Name(string name)
        {
            Name = name;
            return this;
        }

        IInputObjectTypeDescriptor<T> IInputObjectTypeDescriptor<T>.Description(string description)
        {
            Description = description;
            return this;
        }

        IInputObjectTypeDescriptor<T> IInputObjectTypeDescriptor<T>.BindFields(BindingBehavior bindingBehavior)
        {
            _bindingBehavior = bindingBehavior;
            return this;
        }

        IInputFieldDescriptor IInputObjectTypeDescriptor<T>.Field<TValue>(
            Expression<Func<T, TValue>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                InputFieldDescriptor field = new InputFieldDescriptor(p);
                Fields = Fields.Add(field);
                return field;
            }

            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        #endregion
    }
}
