using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal class DictionaryToObjectConverter
        : QueryResultVisitor<DeserializationContext>
    {
        public override void VisitObject(
            ICollection<KeyValuePair<string, object>> dictionary,
            DeserializationContext context)
        {
            if (!context.Type.IsValueType && context.Type != typeof(string))
            {
                ILookup<string, PropertyInfo> properties =
                    context.Type.GetProperties()
                        .ToLookup(t => t.Name,
                            StringComparer.OrdinalIgnoreCase);
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
            DeserializationContext context)
        {
            PropertyInfo property = context.Fields[field.Key].FirstOrDefault();
            if (property != null)
            {
                var valueContext = new DeserializationContext();
                valueContext.Type = property.PropertyType;
                Visit(field.Value, valueContext);
                property.SetValue(context.Object, valueContext.Object);
            }
        }

        protected override void VisitList(
            IList<object> list,
            DeserializationContext context)
        {
            if (context.Type.IsArray)
            {
                var array = Array.CreateInstance(
                    context.Type.GetElementType(),
                    list.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    var valueContext = new DeserializationContext();
                    valueContext.Type = context.Type.GetElementType();
                    Visit(list[i], valueContext);

                    array.SetValue(valueContext.Object, i);
                }

                context.Object = array;
            }
            else
            {
                Type elementType = DotNetTypeInfoFactory.GetInnerListType(context.Type);
                if (elementType != null)
                {
                    Type listType = typeof(List<>).MakeGenericType(elementType);
                    IList l = (IList)Activator.CreateInstance(listType);

                    for (int i = 0; i < list.Count; i++)
                    {
                        var valueContext = new DeserializationContext();
                        valueContext.Type = context.Type.GetElementType();
                        Visit(list[i], valueContext);

                        list.Add(valueContext.Object);
                    }
                }
            }
        }

        protected override void VisitValue(
            object value,
            DeserializationContext context)
        {
            if (value is string s && context.Type.IsEnum)
            {
                context.Object = Enum.Parse(context.Type, s);
            }
            else if (value != null && !context.Type.IsInstanceOfType(value))
            {
                context.Object = Convert.ChangeType(value, context.Type);
            }
            else
            {
                context.Object = value;
            }
        }
    }
}
