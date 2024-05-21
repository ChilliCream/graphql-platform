using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Filters;

internal class FilterGlobalIdInputValueFormatter(
    INodeIdSerializerAccessor serializerAccessor,
    Type namedType)
    : IInputValueFormatter
{
    private INodeIdSerializer? _serializer;

    public object? Format(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return null;
        }

        if (runtimeValue is NodeId id)
        {
            return id.InternalId;
        }

        _serializer ??= serializerAccessor.Serializer;

        if (runtimeValue is string s)
        {
            try
            {
                return _serializer.Parse(s, namedType).InternalId;
            }
            catch (Exception ex) when (ex is not GraphQLException)
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("The ID `{0}` has an invalid format.", s)
                        .Build());
            }
        }

        if (runtimeValue is IEnumerable<NodeId?> nullableIdEnumerable)
        {
            List<object?> list = [];
            foreach (var idv in nullableIdEnumerable)
            {
                if (!idv.HasValue)
                {
                    list.Add(null);
                }
                else
                {
                    list.Add(idv.Value.InternalId);
                }
            }

            return list;
        }

        if (runtimeValue is IEnumerable<NodeId> idEnumerable)
        {
            List<object?> list = [];

            foreach (var idv in idEnumerable)
            {
                list.Add(idv.InternalId);
            }

            return list;
        }

        if (runtimeValue is IEnumerable<string?> stringEnumerable)
        {
            try
            {
                List<object?> list = [];

                foreach (var sv in stringEnumerable)
                {
                    if (sv is null)
                    {
                        list.Add(null);
                        continue;
                    }

                    id = _serializer.Parse(sv, namedType);
                    list.Add(id.InternalId);
                }

                return list;
            }
            catch (Exception ex) when (ex is not GraphQLException)
            {
                throw ThrowHelper.GlobalIdInputValueFormatter_IdsHaveInvalidFormat(stringEnumerable);
            }
        }

        throw ThrowHelper.GlobalIdInputValueFormatter_SpecifiedValueIsNotAValidId();
    }
}
