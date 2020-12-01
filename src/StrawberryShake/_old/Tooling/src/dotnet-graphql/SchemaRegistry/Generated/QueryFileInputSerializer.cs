using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class QueryFileInputSerializer
        : IInputSerializer
    {
        private bool _needsInitialization = true;
        private IValueSerializer? _stringSerializer;
        private IValueSerializer? _hashFormatSerializer;

        public string Name { get; } = "QueryFileInput";

        public ValueKind Kind { get; } = ValueKind.InputObject;

        public Type ClrType => typeof(QueryFileInput);

        public Type SerializationType => typeof(IReadOnlyDictionary<string, object>);

        public void Initialize(IValueSerializerCollection serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.Get("String");
            _hashFormatSerializer = serializerResolver.Get("HashFormat");
            _needsInitialization = false;
        }

        public object? Serialize(object? value)
        {
            if (_needsInitialization)
            {
                throw new InvalidOperationException(
                    $"The serializer for type `{Name}` has not been initialized.");
            }

            if (value is null)
            {
                return null;
            }

            var input = (QueryFileInput)value;
            var map = new Dictionary<string, object?>();

            if (input.Hash.HasValue)
            {
                map.Add("hash", SerializeNullableString(input.Hash.Value));
            }

            if (input.HashAlgorithm.HasValue)
            {
                map.Add("hashAlgorithm", SerializeNullableString(input.HashAlgorithm.Value));
            }

            if (input.HashFormat.HasValue)
            {
                map.Add("hashFormat", SerializeNullableHashFormat(input.HashFormat.Value));
            }

            if (input.Name.HasValue)
            {
                map.Add("name", SerializeNullableString(input.Name.Value));
            }

            if (input.SourceText.HasValue)
            {
                map.Add("sourceText", SerializeNullableString(input.SourceText.Value));
            }

            return map;
        }

        private object? SerializeNullableString(object? value)
        {
            if (value is null)
            {
                return null;
            }


            return _stringSerializer!.Serialize(value);
        }
        private object? SerializeNullableHashFormat(object? value)
        {
            return _hashFormatSerializer!.Serialize(value);
        }

        public object? Deserialize(object? value)
        {
            throw new NotSupportedException(
                "Deserializing input values is not supported.");
        }
    }
}
