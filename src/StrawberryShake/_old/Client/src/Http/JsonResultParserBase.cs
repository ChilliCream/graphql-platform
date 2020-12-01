using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace StrawberryShake.Http
{
    public abstract partial class JsonResultParserBase<T>
        : IResultParser
        where T : class
    {
        private static readonly JsonDocumentOptions _options = new JsonDocumentOptions
        {
            AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip
        };

        protected ReadOnlySpan<byte> TypeName => _typename;

        public Type ResultType => typeof(T);

        public void Parse(ReadOnlySpan<byte> result, IOperationResultBuilder resultBuilder)
        {
            if (resultBuilder is null)
            {
                throw new ArgumentNullException(nameof(resultBuilder));
            }

            byte[] rented = ArrayPool<byte>.Shared.Rent(result.Length);
            result.CopyTo(rented);
            var memory = new ReadOnlyMemory<byte>(rented);
            memory = memory.Slice(0, result.Length);

            using JsonDocument document = JsonDocument.Parse(memory, _options);
            ParseInternal(document, resultBuilder);

            ArrayPool<byte>.Shared.Return(rented);
        }

        public Task ParseAsync(
            Stream stream,
            IOperationResultBuilder resultBuilder,
            CancellationToken cancellationToken)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (resultBuilder is null)
            {
                throw new ArgumentNullException(nameof(resultBuilder));
            }

            return ParseInternalAsync(stream, resultBuilder, cancellationToken);
        }

        private async Task ParseInternalAsync(
            Stream stream,
            IOperationResultBuilder resultBuilder,
            CancellationToken cancellationToken)
        {
            using JsonDocument document = await JsonDocument.ParseAsync(
                stream, _options, cancellationToken)
                .ConfigureAwait(false);
            ParseInternal(document, resultBuilder);
        }

        private void ParseInternal(
            JsonDocument document,
            IOperationResultBuilder resultBuilder)
        {
            if (document.RootElement.TryGetProperty(
                _data, out JsonElement data))
            {
                resultBuilder.SetData(ParserData(data));
            }

            if (document.RootElement.TryGetProperty(
                _errors, out JsonElement errors))
            {
                resultBuilder.AddErrors(ParseErrors(errors));
            }

            if (TryParseExtensions(
                document.RootElement,
                out IReadOnlyDictionary<string, object?>? extensions))
            {
                resultBuilder.AddExtensions(extensions!);
            }

            if (!resultBuilder.IsDataOrErrorModified)
            {
                resultBuilder.AddError(ErrorBuilder.New()
                    .SetMessage(
                        "The specified document is not a valid " +
                        "GraphQL response document. Ensure that either " +
                        "`data` or `errors` os provided. The document " +
                        "parses property names case-sensitive.")
                    .SetCode(ErrorCodes.InvalidResponse)
                    .Build());
            }
        }

        protected abstract T ParserData(JsonElement parent);

        private IEnumerable<IError> ParseErrors(JsonElement parent)
        {
            var length = parent.GetArrayLength();

            for (var i = 0; i < length; i++)
            {
                JsonElement error = parent[i];
                ErrorBuilder builder = ErrorBuilder.New();

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

            var length = list.GetArrayLength();
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

            var length = list.GetArrayLength();
            var pathArray = new object[length];

            for (var i = 0; i < length; i++)
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
            var length = list.GetArrayLength();
            var items = new object?[length];

            for (var i = 0; i < length; i++)
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
