﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            var context = new ConverterContext { ClrType = to };
            Visit(from, context);
            return context.Object;
        }

        protected override void VisitObject(
            IDictionary<string, object> dictionary,
            ConverterContext context)
        {
            if (!context.ClrType.IsValueType
                && context.ClrType != typeof(string))
            {
                ILookup<string, PropertyInfo> properties =
                    context.ClrType.CreatePropertyLookup();

                context.Fields = properties;
                context.Object = Activator.CreateInstance(context.ClrType);

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
                valueContext.ClrType = property.PropertyType;
                Visit(field.Value, valueContext);
                property.SetValue(context.Object, valueContext.Object);
            }
        }

        protected override void VisitList(
            IList<object> list,
            ConverterContext context)
        {
            Type elementType = DotNetTypeInfoFactory
                .GetInnerListType(context.ClrType);

            if (elementType != null)
            {
                Type listType = typeof(List<>).MakeGenericType(elementType);
                IList temp = (IList)Activator.CreateInstance(listType);

                for (int i = 0; i < list.Count; i++)
                {
                    var valueContext = new ConverterContext();
                    valueContext.ClrType = elementType;
                    Visit(list[i], valueContext);

                    temp.Add(valueContext.Object);
                }

                context.Object = context.ClrType.IsAssignableFrom(listType)
                    ? temp
                    : _converter.Convert(listType, context.ClrType, temp);
            }
        }

        protected override void VisitValue(
            object value,
            ConverterContext context)
        {
            context.Object = _converter.Convert(
                typeof(object), context.ClrType, value);
        }
    }
}
