#nullable enable

namespace HotChocolate.Types
{
    public interface IOutputField : IField
    {
        bool IsIntrospectionField { get; }

        bool IsDeprecated { get; }

        string? DeprecationReason { get; }

        IOutputType Type { get; }

        IFieldCollection<IInputField> Arguments { get; }

        new IComplexOutputType DeclaringType { get; }
    }
}
