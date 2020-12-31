using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Relay
{
    public class IdFieldValueSerializer : IFieldValueSerializer
    {
        protected readonly NameString TypeName;
        protected readonly IIdSerializer InnerSerializer;
        protected readonly bool Validate;
        protected readonly bool IsListType;
        protected readonly Type ListType;
        protected readonly Type ValueType;
        protected NameString SchemaName;

        public IdFieldValueSerializer(
            NameString typeName,
            IIdSerializer innerSerializer,
            bool validateType,
            bool isListType,
            Type valueType)
        {
            TypeName = typeName;
            InnerSerializer = innerSerializer;
            Validate = validateType;
            IsListType = isListType;
            ListType = CreateListType(valueType);
            ValueType = valueType;
        }

        public void Initialize(NameString schemaName)
        {
            SchemaName = schemaName;
        }

        public object? Deserialize(object? value)
        {
            if (value is null)
            {
                return null;
            }
            else if (value is string s)
            {
                try
                {
                    IdValue id = DeserializeId(s);

                    if (!Validate || TypeName.Equals(id.TypeName))
                    {
                        return id.Value;
                    }
                }
                catch
                {
                    throw new QueryException(
                        ErrorBuilder.New()
                            .SetMessage("The ID `{0}` has an invalid format.", s)
                            .Build());
                }

                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage("The ID `{0}` is not an ID of `{1}`.", s, TypeName)
                        .Build());
            }
            else if (value is IEnumerable<string> stringEnumerable)
            {
                try
                {
                    var list = (IList)Activator.CreateInstance(ListType);

                    foreach (string sv in stringEnumerable)
                    {
                        IdValue id = DeserializeId(sv);

                        if (!Validate || TypeName.Equals(id.TypeName))
                        {
                            list.Add(id.Value);
                        }
                    }

                    return list;
                }
                catch
                {
                    throw new QueryException(
                        ErrorBuilder.New()
                            .SetMessage(
                                "The IDs `{0}` have an invalid format.", 
                                string.Join(", ", stringEnumerable))
                            .Build());
                }
            }

            throw new QueryException(
                ErrorBuilder.New()
                    .SetMessage("The specified value is not a valid ID value.")
                    .Build());
        }

        protected virtual IdValue DeserializeId(string s)
        {
            return InnerSerializer.Deserialize(s);
        }

        private static Type CreateListType(Type elementType)
        {
            Type listDefinition = typeof(List<>);

            if (elementType == typeof(Guid))
            {
                return listDefinition.MakeGenericType(typeof(Guid));
            }

            if (elementType == typeof(short))
            {
                return listDefinition.MakeGenericType(typeof(short));
            }

            if (elementType == typeof(int))
            {
                return listDefinition.MakeGenericType(typeof(int));
            }

            if (elementType == typeof(long))
            {
                return listDefinition.MakeGenericType(typeof(long));
            }

            return listDefinition.MakeGenericType(typeof(string));
        }

        public virtual object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (IsListType && DotNetTypeInfoFactory.IsListType(value.GetType()))
            {
                var list = new List<object>();

                foreach (object item in (IEnumerable)value)
                {
                    list.Add(InnerSerializer.Serialize(SchemaName, TypeName, item));
                }

                return list;
            }

            return InnerSerializer.Serialize(SchemaName, TypeName, value);
        }

        public IValueNode Rewrite(IValueNode value)
        {
            if (value.Kind == NodeKind.NullValue)
            {
                return value;
            }

            if (value is ListValueNode list)
            {
                var items = new List<IValueNode>();

                foreach (IValueNode item in list.Items)
                {
                    items.Add(Rewrite(item));
                }

                return list.WithItems(items);
            }

            switch (value)
            {
                case StringValueNode stringValue:
                    return stringValue.WithValue(
                        InnerSerializer.Serialize(
                            SchemaName, TypeName, stringValue.Value));

                case IntValueNode intValue:
                    return new StringValueNode(
                        InnerSerializer.Serialize(
                            SchemaName, TypeName, long.Parse(intValue.Value)));

                default:
                    throw new QueryException(
                        ErrorBuilder.New()
                            .SetMessage("The specified literal is not a valid ID value.")
                            .Build());
            }
        }
    }
}
