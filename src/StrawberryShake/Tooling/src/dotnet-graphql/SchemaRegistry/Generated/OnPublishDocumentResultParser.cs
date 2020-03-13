using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake;
using StrawberryShake.Configuration;
using StrawberryShake.Http;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Transport;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnPublishDocumentResultParser
        : JsonResultParserBase<IOnPublishDocument>
    {
        private readonly IValueSerializer _booleanSerializer;
        private readonly IValueSerializer _issueTypeSerializer;
        private readonly IValueSerializer _stringSerializer;
        private readonly IValueSerializer _resolutionTypeSerializer;
        private readonly IValueSerializer _intSerializer;

        public OnPublishDocumentResultParser(IValueSerializerCollection serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _booleanSerializer = serializerResolver.Get("Boolean");
            _issueTypeSerializer = serializerResolver.Get("IssueType");
            _stringSerializer = serializerResolver.Get("String");
            _resolutionTypeSerializer = serializerResolver.Get("ResolutionType");
            _intSerializer = serializerResolver.Get("Int");
        }

        protected override IOnPublishDocument ParserData(JsonElement data)
        {
            return new OnPublishDocument1
            (
                ParseOnPublishDocumentOnPublishDocument(data, "onPublishDocument")
            );

        }

        private global::StrawberryShake.IPublishDocumentEvent ParseOnPublishDocumentOnPublishDocument(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new PublishDocumentEvent
            (
                DeserializeBoolean(obj, "isCompleted"),
                ParseOnPublishDocumentOnPublishDocumentIssue(obj, "issue")
            );
        }

        private global::StrawberryShake.IIssue? ParseOnPublishDocumentOnPublishDocumentIssue(
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

            return new Issue
            (
                DeserializeIssueType(obj, "type"),
                DeserializeString(obj, "code"),
                DeserializeString(obj, "message"),
                DeserializeString(obj, "file"),
                ParseOnPublishDocumentOnPublishDocumentIssueLocation(obj, "location"),
                DeserializeResolutionType(obj, "resolution")
            );
        }

        private global::StrawberryShake.ILocation ParseOnPublishDocumentOnPublishDocumentIssueLocation(
            JsonElement parent,
            string field)
        {
            JsonElement obj = parent.GetProperty(field);

            return new Location
            (
                DeserializeInt(obj, "column"),
                DeserializeInt(obj, "line"),
                DeserializeInt(obj, "start"),
                DeserializeInt(obj, "end")
            );
        }

        private bool DeserializeBoolean(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (bool)_booleanSerializer.Deserialize(value.GetBoolean())!;
        }
        private IssueType DeserializeIssueType(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (IssueType)_issueTypeSerializer.Deserialize(value.GetString())!;
        }

        private string DeserializeString(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (string)_stringSerializer.Deserialize(value.GetString())!;
        }

        private ResolutionType DeserializeResolutionType(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (ResolutionType)_resolutionTypeSerializer.Deserialize(value.GetString())!;
        }
        private int DeserializeInt(JsonElement obj, string fieldName)
        {
            JsonElement value = obj.GetProperty(fieldName);
            return (int)_intSerializer.Deserialize(value.GetInt32())!;
        }
    }
}
