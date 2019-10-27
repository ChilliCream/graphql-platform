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

        public object? Serialize(object? value) // TODO : must be nullable
        {
            if (!_needsInitialization)
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
            map.Add("commentary", SerializeNullableString(input.Commentary));
            map.Add("stars", SerializeInt(input.Stars));
            return map;
        }

        private object? SerializeNullableString(string? value)
        {
            return _stringSerializer!.Serialize(value);
        }

        private object? SerializeInt(int value)
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
