using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal abstract class QueryResultVisitor<TContext>
    {
        public virtual void VisitObject(
            ICollection<KeyValuePair<string, object>> dictionary,
            TContext context)
        {
            foreach (KeyValuePair<string, object> field in dictionary)
            {
                VisitField(field, context);
            }
        }

        protected virtual void VisitField(
            KeyValuePair<string, object> field,
            TContext context)
        {
            Visit(field.Value, context);
        }

        protected virtual void VisitList(IList<object> list, TContext context)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Visit(list[i], context);
            }
        }

        protected virtual void VisitValue(object value, TContext context)
        {

        }

        protected virtual void Visit(object value, TContext context)
        {
            if (value is IDictionary<string, object> dictionary)
            {
                VisitObject(dictionary, context);
            }
            else if (value is IList<object> list)
            {
                VisitList(list, context);
            }
            else
            {
                VisitValue(value, context);
            }
        }
    }

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
                Type elementType = GetInnerListType(context.Type);
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

        private static Type GetInnerListType(Type type)
        {
            if (type.IsInterface && IsSupportedCollectionInterface(type, true))
            {
                return type.GetGenericArguments().First();
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (IsSupportedCollectionInterface(interfaceType))
                {
                    return interfaceType.GetGenericArguments().First();
                }
            }

            return null;
        }

        private static bool IsSupportedCollectionInterface(Type type) =>
            IsSupportedCollectionInterface(type, false);

        private static bool IsSupportedCollectionInterface(
            Type type,
            bool allowEnumerable)
        {
            if (type.IsGenericType)
            {
                Type typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(IReadOnlyCollection<>)
                    || typeDefinition == typeof(IReadOnlyList<>)
                    || typeDefinition == typeof(ICollection<>)
                    || typeDefinition == typeof(IList<>))
                {
                    return true;
                }

                if (allowEnumerable && typeDefinition == typeof(IEnumerable<>))
                {
                    return true;
                }
            }
            return false;
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

    internal class DeserializationContext
    {
        public object Object { get; set; }
        public Type Type { get; set; }
        public ILookup<string, PropertyInfo> Fields { get; set; }
    }
}
