using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal class DictionaryToInputObjectConverter
        : DictionaryVisitor<ConverterContext>
    {
        private readonly ITypeConverter _converter;

        public DictionaryToInputObjectConverter(ITypeConverter converter)
        {
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));
        }

        public object Convert(object from, IInputType to)
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

            Visit(from, context);

            return context.Object;
        }

        protected override void VisitObject(
            IReadOnlyDictionary<string, object> dictionary,
            ConverterContext context)
        {
            if (context.InputType.NamedType() is InputObjectType type)
            {
                Type clrType = type.RuntimeType == typeof(object)
                    ? typeof(Dictionary<string, object>)
                    : type.RuntimeType;

                context.Object = Activator.CreateInstance(clrType);
                context.InputFields = type.Fields;

                foreach (KeyValuePair<string, object> field in dictionary)
                {
                    VisitField(field, context);
                }
            }
        }

        protected override void VisitField(
            KeyValuePair<string, object> field,
            ConverterContext context)
        {
            if (context.InputFields.TryGetField(
                field.Key, out InputField inputField))
            {
                var valueContext = new ConverterContext();
                valueContext.InputType = inputField.Type;
                valueContext.ClrType = inputField.RuntimeType;

                Visit(field.Value, valueContext);

                inputField.SetValue(context.Object, valueContext.Object);
            }
        }

        protected override void VisitList(
            IReadOnlyList<object> list,
            ConverterContext context)
        {
            if (context.InputType.IsListType())
            {
                ListType listType = context.InputType.ListType();
                Type tempType = listType.ToRuntimeType();
                var temp = (IList)Activator.CreateInstance(tempType);

                for (int i = 0; i < list.Count; i++)
                {
                    var valueContext = new ConverterContext();
                    valueContext.InputType = (IInputType)listType.ElementType;
                    valueContext.ClrType = listType.ElementType.ToRuntimeType();

                    Visit(list[i], valueContext);

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
            object value,
            ConverterContext context)
        {
            if (value is null)
            {
                context.Object = null;
            }
            else if (context.InputType.RuntimeType.IsInstanceOfType(value))
            {
                context.Object = value;
            }
            else if (context.InputType.NamedType() is ISerializableType st
                && st.TryDeserialize(value, out object s))
            {
                context.Object = s;
            }
            else
            {
                context.Object = value;
            }

            if (context.Object != null
                && context.Object.GetType() != context.ClrType)
            {
                context.Object = _converter.Convert(
                    context.Object.GetType(),
                    context.ClrType,
                    context.Object);
            }
        }
    }
}
