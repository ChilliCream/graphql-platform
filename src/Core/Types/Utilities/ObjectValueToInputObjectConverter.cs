using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal class ObjectValueToInputObjectConverter
        : SyntaxWalkerBase<IValueNode, ConverterContext>
    {
        private readonly ITypeConversion _converter;

        public ObjectValueToInputObjectConverter(ITypeConversion converter)
        {
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));
        }

        public object Convert(ObjectValueNode from, InputObjectType to)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            var context = new ConverterContext
            {
                InputType = to,
                ClrType = to.ToClrType()
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
                Type clrType = type.ClrType == typeof(object)
                    ? typeof(Dictionary<string, object>)
                    : type.ClrType;

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
                valueContext.ClrType = GetClrType(inputField, context.Object);

                VisitValue(node.Value, valueContext);

                inputField.SetValue(context.Object, valueContext.Object);
            }
        }

        protected override void VisitListValue(
            ListValueNode node,
            ConverterContext context)
        {
            if (context.InputType.IsListType())
            {
                ListType listType = context.InputType.ListType();
                Type tempType = listType.ToClrType();
                IList temp = (IList)Activator.CreateInstance(tempType);

                for (int i = 0; i < node.Items.Count; i++)
                {
                    var valueContext = new ConverterContext();
                    valueContext.InputType = (IInputType)listType.ElementType;
                    valueContext.ClrType = listType.ElementType.ToClrType();

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

        public Type GetClrType(InputField field, object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (field.Property != null)
            {
                return field.Property.PropertyType;
            }

            return typeof(object);
        }
    }
}
