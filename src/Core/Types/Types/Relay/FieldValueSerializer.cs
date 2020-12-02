using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Relay
{
    internal sealed class FieldValueSerializer : IFieldValueSerializer
    {
        private readonly NameString _typeName;
        private readonly IIdSerializer _innerSerializer;
        private readonly bool _validate;
        private readonly bool _list;
        private readonly Type _listType;
        private NameString _schemaName;

        public FieldValueSerializer(
            NameString typeName,
            IIdSerializer innerSerializer,
            bool validateType,
            bool isListType,
            Type valueType)
        {
            _typeName = typeName;
            _innerSerializer = innerSerializer;
            _validate = validateType;
            _list = isListType;
            _listType = CreateListType(valueType);
        }

        public void Initialize(NameString schemaName)
        {
            _schemaName = schemaName;
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
                    IdValue id = _innerSerializer.Deserialize(s);

                    if (!_validate || _typeName.Equals(id.TypeName))
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
                        .SetMessage("The ID `{0}` is not an ID of `{1}`.", s, _typeName)
                        .Build());
            }
            else if (value is IEnumerable<string> stringEnumerable)
            {
                try
                {
                    var list = (IList)Activator.CreateInstance(_listType);

                    foreach (string sv in stringEnumerable)
                    {
                        IdValue id = _innerSerializer.Deserialize(sv);

                        if (!_validate || _typeName.Equals(id.TypeName))
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

        public object? Serialize(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (_list && DotNetTypeInfoFactory.IsListType(value.GetType()))
            {
                var list = new List<object>();

                foreach (object item in (IEnumerable)value)
                {
                    list.Add(_innerSerializer.Serialize(_schemaName, _typeName, item));
                }

                return list;
            }

            return _innerSerializer.Serialize(_schemaName, _typeName, value);
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
                        _innerSerializer.Serialize(
                            _schemaName, _typeName, stringValue.Value));

                case IntValueNode intValue:
                    return new StringValueNode(
                        _innerSerializer.Serialize(
                            _schemaName, _typeName, long.Parse(intValue.Value)));

                default:
                    throw new QueryException(
                        ErrorBuilder.New()
                            .SetMessage("The specified literal is not a valid ID value.")
                            .Build());
            }
        }
    }
}