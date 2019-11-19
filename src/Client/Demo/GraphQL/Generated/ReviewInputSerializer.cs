using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class ReviewInputSerializer
        : IInputSerializer
    {
        private bool _needsInitialization = true;
        private IValueSerializer? _stringSerializer;
        private IValueSerializer? _intSerializer;

        public string Name { get; } = "ReviewInput";

        public ValueKind Kind { get; } = ValueKind.InputObject;

        public Type ClrType => typeof(ReviewInput);

        public Type SerializationType => typeof(IReadOnlyDictionary<string, object>);

        public void Initialize(IValueSerializerResolver serializerResolver)
        {
            if (serializerResolver is null)
            {
                throw new ArgumentNullException(nameof(serializerResolver));
            }
            _stringSerializer = serializerResolver.GetValueSerializer("String");
            _intSerializer = serializerResolver.GetValueSerializer("Int");
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

            var input = (ReviewInput)value;
            var map = new Dictionary<string, object?>();

            if (input.Commentary.HasValue)
            {
                map.Add("commentary", SerializeNullableString(input.Commentary.Value));
            }

            if (input.Stars.HasValue)
            {
                map.Add("stars", SerializeNullableInt(input.Stars.Value));
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
        private object? SerializeNullableInt(object? value)
        {
            return _intSerializer!.Serialize(value);
        }

        public object? Deserialize(object? value)
        {
            throw new NotSupportedException(
                "Deserializing input values is not supported.");
        }
    }
}
