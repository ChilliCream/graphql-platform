using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
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
        }

        public InputValueDefinitionNode SyntaxNode { get; }

        public IValueNode DefaultValue { get; private set; }

        protected PropertyInfo Property { get; private set; }

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
                // TODO : Resources
                throw new InvalidOperationException();
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

            if (!success)
            {
                // TODO : Resources
                throw new InvalidOperationException();
            }

            return value;
        }

        private bool TryGetValueOnUnknownType(object obj, out object value)
        {
            if (obj is IDictionary<string, object> dict)
            {
                dict.TryGetValue(Name, out value);
                return true;
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
            DefaultValue = FieldInitHelper.CreateDefaultValue(
                context, definition, Type);
            Property = definition.Property;
        }
    }
}
