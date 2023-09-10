using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Internal;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Relay;

internal class GlobalIdInputValueFormatter : IInputValueFormatter
{
    private readonly string _typeName;
    private readonly IIdSerializer _idSerializer;
    private readonly bool _validateType;
    private readonly Func<IList> _createList;

    public GlobalIdInputValueFormatter(
        string typeName,
        IIdSerializer idSerializer,
        IExtendedType resultType,
        bool validateType)
    {
        _typeName = typeName;
        _idSerializer = idSerializer;
        _validateType = validateType;
        _createList = CreateListFactory(resultType);
    }

    public object? Format(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return null;
        }

        if (runtimeValue is IdValue id &&
            (!_validateType || _typeName.EqualsOrdinal(id.TypeName)))
        {
            return id.Value;
        }

        if (runtimeValue is string s)
        {
            try
            {
                id = _idSerializer.Deserialize(s);

                if (!_validateType || _typeName.EqualsOrdinal(id.TypeName))
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
                    .SetMessage("The ID `{0}` is not an ID of `{1}`.", s, _typeName)
                    .Build());
        }

        if (runtimeValue is IEnumerable<IdValue?> nullableIdEnumerable)
        {
            var list = _createList();

            foreach (var idv in nullableIdEnumerable)
            {
                if (!idv.HasValue)
                {
                    list.Add(null);
                    continue;
                }

                if (!_validateType || _typeName.EqualsOrdinal(idv.Value.TypeName))
                {
                    list.Add(idv.Value.Value);
                }
            }

            return list;
        }

        if (runtimeValue is IEnumerable<IdValue> idEnumerable)
        {
            var list = _createList();

            foreach (var idv in idEnumerable)
            {
                if (!_validateType || _typeName.EqualsOrdinal(idv.TypeName))
                {
                    list.Add(idv.Value);
                }
            }

            return list;
        }

        if (runtimeValue is IEnumerable<string?> stringEnumerable)
        {
            try
            {
                var list = _createList();

                foreach (var sv in stringEnumerable)
                {
                    if (sv is null)
                    {
                        list.Add(null);
                        continue;
                    }

                    id = _idSerializer.Deserialize(sv);

                    if (!_validateType || _typeName.EqualsOrdinal(id.TypeName))
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
            var listType = typeof(List<>).MakeGenericType(resultType.ElementType!.Source);
            var constructor = listType.GetConstructors().Single(t => t.GetParameters().Length == 0);
            Expression create = Expression.New(constructor);
            return Expression.Lambda<Func<IList>>(create).Compile();
        }

        return () => throw new NotSupportedException("Lists are not supported!");
    }
}
