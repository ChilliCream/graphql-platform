using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace StrawberryShake.Http
{
    public abstract class JsonResultParserBase<T>
        : IResultParser<T>
        where T : class
    {
        private static readonly byte[] _data = new byte[]
        {
            (byte)'d',
            (byte)'a',
            (byte)'t',
            (byte)'a'
        };

        private static readonly byte[] _error = new byte[]
        {
            (byte)'e',
            (byte)'r',
            (byte)'r',
            (byte)'o',
            (byte)'r'
        };

        private static readonly byte[] _extensions = new byte[]
        {
            (byte)'e',
            (byte)'x',
            (byte)'t',
            (byte)'e',
            (byte)'n',
            (byte)'s',
            (byte)'i',
            (byte)'o',
            (byte)'n',
            (byte)'s',
        };

        private static readonly byte[] _typename = new byte[]
        {
            (byte)'_',
            (byte)'_',
            (byte)'t',
            (byte)'y',
            (byte)'p',
            (byte)'e',
            (byte)'n',
            (byte)'a',
            (byte)'m',
            (byte)'e',
        };

        private static readonly byte[] _message = new byte[]
        {
            (byte)'m',
            (byte)'e',
            (byte)'s',
            (byte)'s',
            (byte)'a',
            (byte)'g',
            (byte)'e'
        };

        private static readonly byte[] _locations = new byte[]
        {
            (byte)'l',
            (byte)'o',
            (byte)'c',
            (byte)'a',
            (byte)'t',
            (byte)'i',
            (byte)'o',
            (byte)'n',
            (byte)'s'
        };

        private static readonly byte[] _path = new byte[]
        {
            (byte)'p',
            (byte)'a',
            (byte)'t',
            (byte)'h'
        };

        private static readonly byte[] _line = new byte[]
        {
            (byte)'l',
            (byte)'i',
            (byte)'n',
            (byte)'e'
        };

        private static readonly byte[] _column = new byte[]
        {
            (byte)'c',
            (byte)'o',
            (byte)'l',
            (byte)'u',
            (byte)'m',
            (byte)'n'
        };

        protected ReadOnlySpan<byte> TypeName => _typename;

        public Type ResultType => typeof(T);

        public Task<IOperationResult<T>> ParseAsync(
            Stream stream,
            CancellationToken cancellationToken)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return ParseInternalAsync(stream, cancellationToken);
        }

        async Task<IOperationResult> IResultParser.ParseAsync(
            Stream stream,
            CancellationToken cancellationToken) =>
            await ParseAsync(stream, cancellationToken).ConfigureAwait(false);

        private async Task<IOperationResult<T>> ParseInternalAsync(
            Stream stream,
            CancellationToken cancellationToken)
        {
            using (JsonDocument document = await JsonDocument.ParseAsync(stream)
                .ConfigureAwait(false))
            {
                var builder = OperationResultBuilder.New<T>();

                if (document.RootElement.TryGetProperty(
                    _data, out JsonElement data))
                {
                    builder.SetData(ParserData(data));
                }

                if (document.RootElement.TryGetProperty(
                    _error, out JsonElement errors))
                {
                    builder.AddErrors(ParseErrors(errors));
                }

                if (TryParseExtensions(
                    document.RootElement,
                    out IReadOnlyDictionary<string, object?>? extensions))
                {
                    builder.AddExtensions(extensions!);
                }

                return builder.Build();
            }
        }

        protected abstract T ParserData(JsonElement parent);

        protected IEnumerable<IError> ParseErrors(JsonElement parent)
        {
            int length = parent.GetArrayLength();

            for (int i = 0; i < length; i++)
            {
                JsonElement error = parent[i];
                var builder = ErrorBuilder.New();

                builder.SetMessage(error.GetProperty(_message).GetString());

                if (TryParseLocation(error, out IReadOnlyList<Location>? locations))
                {
                    builder.AddLocations(locations!);
                }

                if (TryParsePath(error, out IReadOnlyList<object>? path))
                {
                    builder.SetPath(path);
                }

                if (TryParseExtensions(error, out IReadOnlyDictionary<string, object?>? extensions))
                {
                    builder.SetExtensions(extensions!);
                }

                yield return builder.Build();
            }
        }

        private bool TryParseLocation(JsonElement error, out IReadOnlyList<Location>? locations)
        {
            if (!error.TryGetProperty(_locations, out JsonElement list)
                || list.ValueKind == JsonValueKind.Null)
            {
                locations = null;
                return false;
            }

            int length = list.GetArrayLength();
            var locs = new Location[length];

            for (int i = 0; i < length; i++)
            {
                JsonElement location = list[i];
                locs[i] = new Location(
                    location.GetProperty(_line).GetInt32(),
                    location.GetProperty(_column).GetInt32());
            }

            locations = locs;
            return true;
        }

        private bool TryParsePath(JsonElement error, out IReadOnlyList<object>? path)
        {
            if (!error.TryGetProperty(_path, out JsonElement list)
                || list.ValueKind == JsonValueKind.Null)
            {
                path = null;
                return false;
            }

            int length = list.GetArrayLength();
            var pathArray = new object[length];

            for (int i = 0; i < length; i++)
            {
                JsonElement element = list[i];
                if (element.ValueKind == JsonValueKind.Number)
                {
                    pathArray[i] = element.GetInt32();
                }
                else
                {
                    pathArray[i] = element.GetString();
                }
            }

            path = pathArray;
            return true;
        }

        private bool TryParseExtensions(
            JsonElement parent,
            out IReadOnlyDictionary<string, object?>? extensions)
        {
            if (!parent.TryGetProperty(_extensions, out JsonElement ext)
                || ext.ValueKind == JsonValueKind.Null)
            {
                extensions = null;
                return false;
            }

            extensions = ParseObject(ext);
            return true;
        }

        private object? ParseValue(JsonElement obj)
        {
            switch (obj.ValueKind)
            {
                case JsonValueKind.Object:
                    return ParseObject(obj);
                case JsonValueKind.Array:
                    return ParseList(obj);
                default:
                    return ParseLeaf(obj);
            }
        }

        private IReadOnlyDictionary<string, object?> ParseObject(JsonElement obj)
        {
            var dict = new Dictionary<string, object?>();

            foreach (JsonProperty property in obj.EnumerateObject())
            {
                dict[property.Name] = ParseValue(property.Value);
            }

            return dict;
        }

        private IReadOnlyList<object?> ParseList(JsonElement list)
        {
            int length = list.GetArrayLength();
            var items = new object?[length];

            for (int i = 0; i < length; i++)
            {
                items[i] = ParseValue(list[i]);
            }

            return items;
        }

        private object? ParseLeaf(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Number:
                    return value.GetInt32();
                case JsonValueKind.String:
                    return value.GetString();
                default:
                    return value.GetRawText();
            }
        }
    }
}
