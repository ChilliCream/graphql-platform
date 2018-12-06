using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Utilities
{
    internal class DictionaryToObjectConverter
        : DictionaryVisitor<ConverterContext>
    {
        private readonly ITypeConversion _converter;

        public DictionaryToObjectConverter(ITypeConversion converter)
        {
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));
        }

        public object Convert(object from, Type to)
        {
            var context = new ConverterContext { Type = to };
            Visit(from, context);
            return context.Object;
        }

        protected override void VisitObject(
            IDictionary<string, object> dictionary,
            ConverterContext context)
        {
            if (!context.Type.IsValueType && context.Type != typeof(string))
            {
                ILookup<string, PropertyInfo> properties =
                    context.Type.CreatePropertyLookup();

                context.Fields = properties;
                context.Object = Activator.CreateInstance(context.Type);

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
            PropertyInfo property = context.Fields[field.Key].FirstOrDefault();
            if (property != null)
            {
                var valueContext = new ConverterContext();
                valueContext.Type = property.PropertyType;
                Visit(field.Value, valueContext);
                property.SetValue(context.Object, valueContext.Object);
            }
        }

        protected override void VisitList(
            IList<object> list,
            ConverterContext context)
        {
            Type elementType = DotNetTypeInfoFactory
                .GetInnerListType(context.Type);

            if (elementType != null)
            {
                Type listType = typeof(List<>).MakeGenericType(elementType);
                IList temp = (IList)Activator.CreateInstance(listType);

                for (int i = 0; i < list.Count; i++)
                {
                    var valueContext = new ConverterContext();
                    valueContext.Type = elementType;
                    Visit(list[i], valueContext);

                    temp.Add(valueContext.Object);
                }

                context.Object = context.Type.IsAssignableFrom(listType)
                    ? temp
                    : _converter.Convert(listType, context.Type, temp);
            }
        }

        protected override void VisitValue(
            object value,
            ConverterContext context)
        {
            context.Object = _converter.Convert(
                typeof(object), context.Type, value);
        }
    }
}
