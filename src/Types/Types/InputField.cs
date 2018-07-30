using System;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputField
        : FieldBase<IInputType>
        , IInputField
    {
        private readonly object _nativeDefaultValue;

        internal InputField(ArgumentDescription argumentDescription)
            : base(argumentDescription)
        {
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

        public IValueNode DefaultValue { get; private set; }

        public PropertyInfo Property { get; private set; }

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
                    && context.TryGetProperty(context.Type, Name, out PropertyInfo property))
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
