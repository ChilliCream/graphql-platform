using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputField
        : FieldBase
        , IInputField
    {
        private readonly TypeReference _typeReference;
        private object _nativeDefaultValue;

        internal InputField(ArgumentDescription argumentDescription)
            : base(argumentDescription?.Name, argumentDescription?.Description)
        {
            if (argumentDescription == null)
            {
                throw new ArgumentNullException(nameof(argumentDescription));
            }

            _typeReference = argumentDescription.TypeReference;
            _nativeDefaultValue = argumentDescription.NativeDefaultValue;

            SyntaxNode = argumentDescription.SyntaxNode;
            DefaultValue = argumentDescription.DefaultValue;
        }

        internal InputField(InputFieldDescription inputFieldDescription)
            : this((ArgumentDescription)inputFieldDescription)
        {
            Property = inputFieldDescription.Property;
        }

        public InputValueDefinitionNode SyntaxNode { get; }

        public IInputType Type { get; private set; }

        public IValueNode DefaultValue { get; private set; }

        public PropertyInfo Property { get; private set; }

        #region Initialization

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            if (_typeReference != null)
            {
                context.RegisterType(_typeReference);
            }
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            Type = context.ResolveFieldType<IInputType>(this, _typeReference);
            if (Type != null)
            {
                CompleteDefaultValue(context, Type);

                if (context.Type is InputObjectType
                    && Property == null
                    && context.TryGetProperty(context.Type, out PropertyInfo property))
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
                        DefaultValue = new NullValueNode();
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
