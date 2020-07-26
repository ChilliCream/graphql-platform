using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class InputField
        : FieldBase<IInputType, InputFieldDefinition>
        , IInputField
        , IHasClrType
    {
        public InputField(InputFieldDefinition definition)
            : base(definition)
        {
            SyntaxNode = definition.SyntaxNode;
            DefaultValue = definition.DefaultValue;
            Property = definition.Property;

            Type propertyType = definition.Property?.PropertyType;

            if (propertyType is { }
                && propertyType.IsGenericType
                && propertyType.GetGenericTypeDefinition() == typeof(Optional<>))
            {
                IsOptional = true;
            }
        }

        public InputValueDefinitionNode SyntaxNode { get; }

        public IFieldValueSerializer Serializer { get; private set; }

        public IValueNode DefaultValue { get; private set; }

        internal protected PropertyInfo Property { get; }

        internal protected bool IsOptional { get; }

        public new InputObjectType DeclaringType =>
            (InputObjectType)base.DeclaringType;

        public override Type ClrType
        {
            get
            {
                return Property == null
                    ? base.ClrType
                    : Property.PropertyType;
            }
        }

        public void SetValue(object obj, object value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            bool success = Property == null
                ? TrySetValueOnUnknownType(obj, value)
                : TrySetValueOnKnownType(obj, value);

            if (!success)
            {
                throw new InvalidOperationException(
                    TypeResources.InputField_CannotSetValue);
            }
        }

        private bool TrySetValueOnUnknownType(object obj, object value)
        {
            if (obj is IDictionary<string, object> dict)
            {
                dict[Name] = value;
                return true;
            }

            ILookup<string, PropertyInfo> properties =
                ReflectionUtils.CreatePropertyLookup(obj.GetType());
            PropertyInfo property = properties[Name].FirstOrDefault();

            if (property != null)
            {
                property.SetValue(obj, value);
                return true;
            }

            return false;
        }

        private bool TrySetValueOnKnownType(object obj, object value)
        {
            Property.SetValue(obj, value);
            return true;
        }

        public object GetValue(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            bool success = Property == null
                ? TryGetValueOnUnknownType(obj, out object value)
                : TryGetValueOnKnownType(obj, out value);

            return success ? value : null;
        }

        public bool TryGetValue(object obj, out object value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return Property == null
                ? TryGetValueOnUnknownType(obj, out value)
                : TryGetValueOnKnownType(obj, out value);
        }

        private bool TryGetValueOnUnknownType(object obj, out object value)
        {
            if (obj is IDictionary<string, object> d)
            {
                return d.TryGetValue(Name, out value);
            }

            ILookup<string, PropertyInfo> properties =
                ReflectionUtils.CreatePropertyLookup(obj.GetType());
            PropertyInfo property = properties[Name].FirstOrDefault();

            if (property != null)
            {
                value = property.GetValue(obj);
                return true;
            }

            value = null;
            return false;
        }

        private bool TryGetValueOnKnownType(object obj, out object value)
        {
            value = Property.GetValue(obj);
            return true;
        }

        protected override void OnCompleteField(
            ICompletionContext context,
            InputFieldDefinition definition)
        {
            base.OnCompleteField(context, definition);
            Serializer = definition.Serializer;
            DefaultValue = FieldInitHelper.CreateDefaultValue(context, definition, Type);
        }
    }
}
