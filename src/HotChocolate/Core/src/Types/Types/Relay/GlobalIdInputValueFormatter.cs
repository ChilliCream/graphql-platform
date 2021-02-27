using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Types.Relay
{
    internal class GlobalIdInputValueFormatter : IInputValueFormatter
    {
        public readonly NameString TypeName;
        private readonly IIdSerializer _idSerializer;
        private readonly bool _validateType;
        private readonly Func<IList> _createList;

        public GlobalIdInputValueFormatter(
            NameString typeName,
            IIdSerializer idSerializer,
            IExtendedType resultType,
            bool validateType)
        {
            TypeName = typeName;
            _idSerializer = idSerializer;
            _validateType = validateType;
            _createList = CreateListFactory(resultType);
        }

        public object? OnAfterDeserialize(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return null;
            }

            if (runtimeValue is IdValue id &&
                (!_validateType || TypeName.Equals(id.TypeName)))
            {
                return id.Value;
            }

            if (runtimeValue is string s)
            {
                try
                {
                    id = _idSerializer.Deserialize(s);

                    if (!_validateType || TypeName.Equals(id.TypeName))
                    {
                        return id.Value;
                    }
                }
                catch
                {
                    throw new GraphQLException(
                        ErrorBuilder.New()
                            .SetMessage("The ID `{0}` has an invalid format.", s)
                            .Build());
                }

                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("The ID `{0}` is not an ID of `{1}`.", s, TypeName)
                        .Build());
            }

            if (runtimeValue is IEnumerable<IdValue> idEnumerable)
            {
                IList list = _createList();

                foreach (IdValue idv in idEnumerable)
                {
                    if (!_validateType || TypeName.Equals(idv.TypeName))
                    {
                        list.Add(idv.Value);
                    }
                }

                return list;
            }

            if (runtimeValue is IEnumerable<string> stringEnumerable)
            {
                try
                {
                    IList list = _createList();

                    foreach (string sv in stringEnumerable)
                    {
                        id = _idSerializer.Deserialize(sv);

                        if (!_validateType || TypeName.Equals(id.TypeName))
                        {
                            list.Add(id.Value);
                        }
                    }

                    return list;
                }
                catch
                {
                    throw new GraphQLException(
                        ErrorBuilder.New()
                            .SetMessage(
                                "The IDs `{0}` have an invalid format.",
                                string.Join(", ", stringEnumerable))
                            .Build());
                }
            }

            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The specified value is not a valid ID value.")
                    .Build());
        }

        private static Func<IList> CreateListFactory(IExtendedType resultType)
        {
            if (resultType.IsArrayOrList)
            {
                Type listType = typeof(List<>).MakeGenericType(resultType.ElementType!.Source);
                ConstructorInfo constructor = listType.GetConstructors().Single(t => t.GetParameters().Length == 0);
                Expression create = Expression.New(constructor);
                return Expression.Lambda<Func<IList>>(create).Compile();
            }

            return () => throw new NotSupportedException("Lists are not supported!");
        }
    }
}
