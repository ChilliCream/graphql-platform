using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Applies the @eventCursor directive. On an argument it marks the resume input that
/// the distributed GraphQL executor uses to continue an event stream. On an output
/// field it marks the cursor that carries the position within the stream.
/// </para>
/// <para>
/// @eventCursor
/// </para>
/// </summary>
[AttributeUsage(
    AttributeTargets.Parameter
    | AttributeTargets.Property,
    AllowMultiple = false)]
public sealed class EventCursorAttribute : DescriptorAttribute
{
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        switch (descriptor)
        {
            case IArgumentDescriptor arg:
                arg.EventCursor();
                break;

            case IObjectFieldDescriptor field:
                field.EventCursor();
                break;

            default:
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(
                            "EventCursor directive is only supported on arguments and "
                            + "field definitions of object types.")
                        .SetExtension("member", attributeProvider)
                        .SetExtension("descriptor", descriptor)
                        .Build());
        }
    }
}
