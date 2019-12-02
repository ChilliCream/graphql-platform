using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Http;

namespace StrawberryShake.Client.GitHub
{
    public class GetUserResultParser
        : JsonResultParserBase<IGetUser>
    {
        private readonly IValueSerializer _stringSerializer;
        private readonly IValueSerializer _dateTimeSerializer;
        private readonly IValueSerializer _intSerializer;

        public GetUserResultParser(IValueSerializerResolver serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.GetValueSerializer("String");
            _dateTimeSerializer = serializerResolver.GetValueSerializer("DateTime");
            _intSerializer = serializerResolver.GetValueSerializer("Int");
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

            if (obj.ValueKind == JsonValueKind.Null)
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

            return (string?)_stringSerializer.Deserialize(value.GetString())!;
        }

        private System.DateTimeOffset DeserializeDateTime(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (System.DateTimeOffset)_dateTimeSerializer.Deserialize(value.GetString())!;
        }
        private int DeserializeInt(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (int)_intSerializer.Deserialize(value.GetInt32())!;
        }
    }
}
