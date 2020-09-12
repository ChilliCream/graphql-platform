using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal class ObjectValueToInputObjectConverter
        : SyntaxWalkerBase<IValueNode, ConverterContext>
    {
        private readonly ITypeConverter _converter;

        public ObjectValueToInputObjectConverter(ITypeConverter converter)
        {
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));
        }

        public object Convert(ObjectValueNode from, InputObjectType to)
        {
            if (from is null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to is null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            var context = new ConverterContext
            {
                InputType = to,
                ClrType = to.ToRuntimeType()
            };

            VisitObjectValue(from, context);

            return context.Object;
        }

        protected override void VisitObjectValue(
            ObjectValueNode node,
            ConverterContext context)
        {
            if (context.InputType.NamedType() is InputObjectType type)
            {
                Type clrType = type.RuntimeType == typeof(object)
                    ? typeof(Dictionary<string, object>)
                    : type.RuntimeType;

                context.Object = Activator.CreateInstance(clrType);
                context.InputFields = type.Fields;

                foreach (ObjectFieldNode field in node.Fields)
                {
                    VisitObjectField(field, context);
                }
            }
        }

        protected override void VisitObjectField(
            ObjectFieldNode node,
            ConverterContext context)
        {
            if (context.InputFields.TryGetField(
                node.Name.Value, out InputField inputField))
            {
                var valueContext = new ConverterContext();
                valueContext.InputType = inputField.Type;
                valueContext.ClrType = inputField.RuntimeType;

                VisitValue(node.Value, valueContext);

                object value = (inputField.RuntimeType != null
                    && !inputField.RuntimeType
                        .IsInstanceOfType(valueContext.Object)
                    && _converter.TryConvert(
                        typeof(object), inputField.RuntimeType,
                        valueContext.Object, out object obj))
                    ? obj
                    : valueContext.Object;

                inputField.SetValue(context.Object, value);
            }
        }

        protected override void VisitListValue(
            ListValueNode node,
            ConverterContext context)
        {
            if (context.InputType.IsListType())
            {
                ListType listType = context.InputType.ListType();
                Type tempType = listType.ToRuntimeType();
                var temp = (IList)Activator.CreateInstance(tempType);

                for (int i = 0; i < node.Items.Count; i++)
                {
                    var valueContext = new ConverterContext();
                    valueContext.InputType = (IInputType)listType.ElementType;
                    valueContext.ClrType = listType.ElementType.ToRuntimeType();

                    VisitValue(node.Items[i], valueContext);

                    temp.Add(valueContext.Object);
                }

                Type expectedType = context.ClrType == typeof(object)
                    ? typeof(List<object>)
                    : context.ClrType;

                context.Object = expectedType.IsAssignableFrom(tempType)
                    ? temp
                    : _converter.Convert(tempType, expectedType, temp);
            }
        }

        protected override void VisitValue(
            IValueNode node,
            ConverterContext context)
        {
            if (node is null)
            {
                return;
            }

            switch (node)
            {
                case ListValueNode value:
                    VisitListValue(value, context);
                    break;
                case ObjectValueNode value:
                    VisitObjectValue(value, context);
                    break;
                case VariableNode value:
                    VisitVariable(value, context);
                    break;
                default:
                    context.Object = context.InputType.ParseLiteral(node);
                    break;
            }
        }
    }
}
