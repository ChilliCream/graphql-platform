using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IInputSerializer
        : IValueSerializer
    {
        void Initialize(IValueSerializerCollection serializerResolver);
    }
}
