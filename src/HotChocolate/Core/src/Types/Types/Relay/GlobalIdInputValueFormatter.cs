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
    private readonly INodeIdSerializerAccessor _serializerAccessor;
    private readonly string _typeName;
    private readonly bool _validateType;
    private readonly Func<IList> _createList;
    private INodeIdSerializer? _serializer;

    public GlobalIdInputValueFormatter(
        string typeName,
        INodeIdSerializerAccessor serializerAccessor,
        IExtendedType resultType,
        bool validateType)
    {
        _typeName = typeName;
        _serializerAccessor = serializerAccessor;
        _validateType = validateType;
        _createList = CreateListFactory(resultType);
    }

    public object? Format(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return null;
        }

        _serializer ??= _serializerAccessor.Serializer;

        if (runtimeValue is NodeId id &&
            (!_validateType || _typeName.EqualsOrdinal(id.TypeName)))
        {
            return id.InternalId;
        }

        if (runtimeValue is string s)
        {
            try
            {
                id = _serializer.Parse(s);
                if (!_validateType || _typeName.EqualsOrdinal(id.TypeName))
                {
                    return id.InternalId;
                }
            }
            catch(Exception ex) when (ex is not GraphQLException)
            {
                // todo : resources
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("The ID `{0}` has an invalid format.", s)
                        .Build());
            }

            // todo : resources
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The ID `{0}` is not an ID of `{1}`.", s, _typeName)
                    .Build());
        }

        if (runtimeValue is IEnumerable<NodeId?> nullableIdEnumerable)
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
                    list.Add(idv.Value.InternalId);
                }
            }

            return list;
        }

        if (runtimeValue is IEnumerable<NodeId> idEnumerable)
        {
            var list = _createList();

            foreach (var idv in idEnumerable)
            {
                if (!_validateType || _typeName.EqualsOrdinal(idv.TypeName))
                {
                    list.Add(idv.InternalId);
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

                    id = _serializer.Parse(sv);

                    if (!_validateType || _typeName.EqualsOrdinal(id.TypeName))
                    {
                        list.Add(id.InternalId);
                    }
                }

                return list;
            }
            catch(Exception ex) when (ex is not GraphQLException)
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

    // TODO : AOT once we have the new serializer in we should be able to get rid of this.
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
