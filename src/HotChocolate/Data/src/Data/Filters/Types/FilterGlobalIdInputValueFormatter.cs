using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Filters;

internal class FilterGlobalIdInputValueFormatter(
    INodeIdSerializerAccessor serializerAccessor,
    Type namedType,
    IDictionary<string, Type>? nodeIdTypes = null)
    : IInputValueFormatter
{
    private INodeIdSerializer? _serializer;

    public object? Format(object? originalValue)
    {
        if (originalValue is null)
        {
            return null;
        }

        if (originalValue is NodeId id)
        {
            return id.InternalId;
        }

        _serializer ??= serializerAccessor.Serializer;

        if (originalValue is string s)
        {
            try
            {
                return _serializer.Parse(s, ResolveNamedType(s)).InternalId;
            }
            catch (Exception ex) when (ex is not GraphQLException)
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("The ID `{0}` has an invalid format.", s)
                        .Build());
            }
        }

        if (originalValue is IEnumerable<NodeId?> nullableIdEnumerable)
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

        if (originalValue is IEnumerable<NodeId> idEnumerable)
        {
            List<object?> list = [];

            foreach (var idv in idEnumerable)
            {
                list.Add(idv.InternalId);
            }

            return list;
        }

        if (originalValue is IEnumerable<string?> stringEnumerable)
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

                    id = _serializer.Parse(sv, ResolveNamedType(sv));
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

    private Type ResolveNamedType(string formattedId)
    {
        if (namedType != typeof(string)
            || nodeIdTypes is null
            || nodeIdTypes.Count == 0
            || _serializer is null)
        {
            return namedType;
        }

        try
        {
            var parsed = _serializer.Parse(formattedId, typeof(string));
            return nodeIdTypes.TryGetValue(parsed.TypeName, out var runtimeType)
                ? runtimeType
                : namedType;
        }
        catch (Exception ex) when (ex is not GraphQLException)
        {
            return namedType;
        }
    }
}
