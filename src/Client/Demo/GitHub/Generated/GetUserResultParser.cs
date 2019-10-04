using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Http;

namespace  StrawberryShake.Client.GitHub
{
    public class GetUserResultParser
        : JsonResultParserBase<IGetUser>
    {
        private readonly IValueSerializer _stringSerializer;
        private readonly IValueSerializer _dateTimeSerializer;
        private readonly IValueSerializer _intSerializer;

        public GetUserResultParser(IEnumerable<IValueSerializer> serializers)
        {
            IReadOnlyDictionary<string, IValueSerializer> map = serializers.ToDictionary();

            if (!map.TryGetValue("String", out IValueSerializer? serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `String`.",
                    nameof(serializers));
            }
            _stringSerializer = serializer;

            if (!map.TryGetValue("DateTime", out  serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `DateTime`.",
                    nameof(serializers));
            }
            _dateTimeSerializer = serializer;

            if (!map.TryGetValue("Int", out  serializer))
            {
                throw new ArgumentException(
                    "There is no serializer specified for `Int`.",
                    nameof(serializers));
            }
            _intSerializer = serializer;
        }

        protected override IGetUser ParserData(JsonElement data)
        {
            return new GetUser
            (
                ParseGetUserUser(data, "user")
            );

        }

        private IUser? ParseGetUserUser(
            JsonElement parent,
            string field)
        {
            if (!parent.TryGetProperty(field, out JsonElement obj))
            {
                return null;
            }

            return new User
            (
                DeserializeNullableString(obj, "name"),
                DeserializeNullableString(obj, "company"),
                DeserializeDateTime(obj, "createdAt"),
                ParseGetUserUserFollowers(obj, "followers")
            );
        }

        private IFollowerConnection ParseGetUserUserFollowers(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new FollowerConnection
            (
                DeserializeInt(obj, "totalCount")
            );
        }

        private string? DeserializeNullableString(JsonElement obj, string fieldName)
        {
            if (!obj.TryGetProperty(fieldName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return (string?)_stringSerializer.Serialize(value.GetString())!;
        }

        private System.DateTimeOffset DeserializeDateTime(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (System.DateTimeOffset)_dateTimeSerializer.Serialize(value.GetString())!;
        }
        private int DeserializeInt(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (int)_intSerializer.Serialize(value.GetInt32())!;
        }
    }
}
