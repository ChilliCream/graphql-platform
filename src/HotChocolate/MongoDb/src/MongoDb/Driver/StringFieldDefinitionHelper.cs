using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace HotChocolate.MongoDb.Data
{
    /// <summary>
    /// This class was ported over from the official mongo db driver
    /// </summary>
    internal static class StringFieldDefinitionHelper
    {
        public static void Resolve(
            string fieldName,
            IBsonSerializer serializer,
            out string? resolvedFieldName,
            out IBsonSerializer? resolvedFieldSerializer)
        {
            BsonSerializationInfo serializationInfo;

            resolvedFieldName = fieldName;
            resolvedFieldSerializer = null;

            var documentSerializer = serializer as IBsonDocumentSerializer;
            if (serializer is IBsonArraySerializer bsonArraySerializer &&
                bsonArraySerializer.TryGetItemSerializationInfo(out serializationInfo))
            {
                documentSerializer = serializationInfo.Serializer as IBsonDocumentSerializer;
            }

            if (documentSerializer == null)
            {
                return;
            }

            // shortcut BsonDocumentSerializer since it is so common
            if (serializer.GetType() == typeof(BsonDocumentSerializer))
            {
                return;
            }

            // first, lets try the quick and easy one, which will be a majority of cases
            if (documentSerializer.TryGetMemberSerializationInfo(fieldName, out serializationInfo))
            {
                resolvedFieldName = serializationInfo.ElementName;
                resolvedFieldSerializer = serializationInfo.Serializer;
                return;
            }

            // now lets go and do the more difficult variant
            string[] nameParts = fieldName.Split('.');
            if (nameParts.Length <= 1)
            {
                // if we only have 1, then it's no different than what we did above
                // when we found nothing.
                return;
            }

            IBsonArraySerializer? arraySerializer;
            resolvedFieldSerializer = documentSerializer;
            for (int i = 0; i < nameParts.Length; i++)
            {
                if (nameParts[i] == "$" || nameParts[i].All(char.IsDigit))
                {
                    arraySerializer = resolvedFieldSerializer as IBsonArraySerializer;
                    if (resolvedFieldSerializer is IBsonArraySerializer &&
                        arraySerializer is {} &&
                        arraySerializer.TryGetItemSerializationInfo(out serializationInfo))
                    {
                        resolvedFieldSerializer = serializationInfo.Serializer;
                        continue;
                    }

                    resolvedFieldSerializer = null;
                    break;
                }

                documentSerializer = resolvedFieldSerializer as IBsonDocumentSerializer;
                if (documentSerializer == null ||
                    !documentSerializer.TryGetMemberSerializationInfo(
                        nameParts[i],
                        out serializationInfo))
                {
                    // need to check if this is an any element array match
                    arraySerializer = resolvedFieldSerializer as IBsonArraySerializer;
                    if (arraySerializer != null &&
                        arraySerializer.TryGetItemSerializationInfo(out serializationInfo))
                    {
                        documentSerializer =
                            serializationInfo.Serializer as IBsonDocumentSerializer;
                        if (documentSerializer == null ||
                            !documentSerializer.TryGetMemberSerializationInfo(
                                nameParts[i],
                                out serializationInfo))
                        {
                            resolvedFieldSerializer = null;
                            break;
                        }
                    }
                    else
                    {
                        resolvedFieldSerializer = null;
                        break;
                    }
                }

                nameParts[i] = serializationInfo.ElementName;
                resolvedFieldSerializer = serializationInfo.Serializer;
            }

            resolvedFieldName = string.Join(".", nameParts);
        }
    }
}
