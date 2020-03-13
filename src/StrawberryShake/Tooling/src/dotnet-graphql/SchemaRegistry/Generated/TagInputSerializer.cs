using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class TagInputSerializer
        : IInputSerializer
    {
        private bool _needsInitialization = true;
        private IValueSerializer? _stringSerializer;

        public string Name { get; } = "TagInput";

        public ValueKind Kind { get; } = ValueKind.InputObject;

        public Type ClrType => typeof(TagInput);

        public Type SerializationType => typeof(IReadOnlyDictionary<string, object>);

        public void Initialize(IValueSerializerCollection serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.Get("String");
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

            var input = (TagInput)value;
            var map = new Dictionary<string, object?>();

            if (input.Key.HasValue)
            {
                map.Add("key", SerializeNullableString(input.Key.Value));
            }

            if (input.Value.HasValue)
            {
                map.Add("value", SerializeNullableString(input.Value.Value));
            }

            return map;
        }

        private object? SerializeNullableString(object? value)
        {
            return _stringSerializer!.Serialize(value);
        }

        public object? Deserialize(object? value)
        {
            throw new NotSupportedException(
                "Deserializing input values is not supported.");
        }
    }
}
