using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public static class SerializerUtilities
    {
        public static IReadOnlyDictionary<string, IValueSerializer> ToDictionary(
            this IEnumerable<IValueSerializer> serializers)
        {
            if (serializers is null)
            {
                throw new ArgumentNullException(nameof(serializers));
            }

            var map = new Dictionary<string, IValueSerializer>();

            foreach (IValueSerializer serializer in serializers)
            {
                if (!map.ContainsKey(serializer.Name))
                {
                    map.Add(serializer.Name, serializer);
                }
            }

            return map;
        }
    }
}
