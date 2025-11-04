using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

public sealed class SerializeAsAttribute : ScalarTypeDescriptorAttribute
{
    public SerializeAsAttribute(ScalarSerializationType type, string? pattern = null)
    {
        if (type is ScalarSerializationType.Undefined)
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "The type is undefined.");
        }

        if ((ScalarSerializationType.String & type) != ScalarSerializationType.String
            && !string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentException(
                "A pattern can only be specified when the scalar serializes as string.",
                nameof(pattern));
        }

        Type = type;
        Pattern = pattern;
    }

    public ScalarSerializationType Type { get; }

    public string? Pattern { get; }

    protected override void OnConfigure(
        IDescriptorContext context,
        IScalarTypeDescriptor descriptor,
        Type? type)
    {
        if (context.Options.ApplySerializeAsToScalars)
        {
            descriptor.Directive(new SerializeAs(Type, Pattern));
        }
    }
}
