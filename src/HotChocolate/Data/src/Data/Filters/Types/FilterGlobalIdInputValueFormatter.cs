using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Filters;

internal class FilterGlobalIdInputValueFormatter : IInputValueFormatter
{
    private readonly IIdSerializer _idSerializer;

    public FilterGlobalIdInputValueFormatter(IIdSerializer idSerializer)
    {
        _idSerializer = idSerializer;
    }

    public object? Format(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return null;
        }

        if (runtimeValue is IdValue id)
        {
            return id.Value;
        }

        if (runtimeValue is string s)
        {
            try
            {
                return _idSerializer.Deserialize(s).Value;
            }
            catch
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("The ID `{0}` has an invalid format.", s)
                        .Build());
            }
        }

        if (runtimeValue is IEnumerable<IdValue?> nullableIdEnumerable)
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
                    list.Add(idv.Value.Value);
                }
            }

            return list;
        }

        if (runtimeValue is IEnumerable<IdValue> idEnumerable)
        {
            List<object?> list = [];

            foreach (var idv in idEnumerable)
            {
                list.Add(idv.Value);
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

                    id = _idSerializer.Deserialize(sv);

                    list.Add(id.Value);
                }

                return list;
            }
            catch
            {
                throw ThrowHelper
                    .GlobalIdInputValueFormatter_IdsHaveInvalidFormat(stringEnumerable);
            }
        }

        throw ThrowHelper.GlobalIdInputValueFormatter_SpecifiedValueIsNotAValidId();
    }
}
