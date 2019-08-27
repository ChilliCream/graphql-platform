using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IOperation
    {
        string Name { get; }

        IDocument Document { get; }

        IReadOnlyDictionary<string, object> GetVariables(
            IEnumerable<IValueSerializer> serializers);
    }

    public interface IOperation<out T>
        : IOperation
    {
    }
}
