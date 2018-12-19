using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class InputField
        : FieldBase<IInputType>
        , IInputField
    {
        private readonly object _nativeDefaultValue;

        private InputField(
            ArgumentDescription argumentDescription,
            DirectiveLocation location)
            : base(argumentDescription, location)
        {
            _nativeDefaultValue = argumentDescription.NativeDefaultValue;
            SyntaxNode = argumentDescription.SyntaxNode;
            DefaultValue = argumentDescription.DefaultValue;
        }

        internal InputField(ArgumentDescription argumentDescription)
            : this(argumentDescription, DirectiveLocation.ArgumentDefinition)
        {
        }

        internal InputField(InputFieldDescription inputFieldDescription)
            : this(inputFieldDescription,
                DirectiveLocation.InputFieldDefinition)
        {
            Property = inputFieldDescription.Property;
        }

        public InputValueDefinitionNode SyntaxNode { get; }

        public IValueNode DefaultValue { get; private set; }

        // TODO : make this private
        public PropertyInfo Property { get; private set; }

        public void SetValue(object obj, object value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (DeclaringType is InputObjectType type)
            {
                bool success = Property == null
                    ? TrySetValueOnUnknownType(obj, value)
                    : TrySetValueOnKnownType(obj, value);

                if (!success)
                {
                    // TODO : Resources
                    throw new InvalidOperationException();
                }
            }
            else
            {
                throw new NotSupportedException();
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

            if (DeclaringType is InputObjectType type)
            {
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
            else
            {
                throw new NotSupportedException();
            }
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

        #region Initialization

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            if (Type != null)
            {
                CompleteDefaultValue(context, Type);

                if (context.Type is InputObjectType
                    && Property == null
                    && context.TryGetProperty(
                        context.Type, Name,
                        out PropertyInfo property))
                {
                    Property = property;
                }
            }
        }

        private void CompleteDefaultValue(
            ITypeInitializationContext context,
            IInputType type)
        {
            try
            {
                if (DefaultValue == null)
                {
                    if (_nativeDefaultValue == null)
                    {
                        DefaultValue = NullValueNode.Default;
                    }
                    else
                    {
                        DefaultValue = type.ParseValue(_nativeDefaultValue);
                    }
                }
            }
            catch (Exception ex)
            {
                context.ReportError(new SchemaError(
                    "Could not parse the native value of input field " +
                    $"`{context.Type.Name}.{Name}`.", context.Type, ex));
            }
        }

        #endregion
    }
}
