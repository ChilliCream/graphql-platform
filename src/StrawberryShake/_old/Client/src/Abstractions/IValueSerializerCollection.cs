using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IValueSerializerCollection
        : IReadOnlyCollection<IValueSerializer>
    {
        /// <summary>
        /// Gets the value
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        IValueSerializer Get(string typeName);
    }
}
