using System.Reflection;
using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// The @key directive is used to indicate a combination of fields that
/// can be used to uniquely identify and fetch an object or interface.
/// <example>
/// type Product @key(fields: "upc") {
///   upc: UPC!
///   name: String
/// }
/// </example>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface |
    AttributeTargets.Property |
    AttributeTargets.Method,
    AllowMultiple = true)]
public sealed class KeyAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="KeyAttribute"/>.
    /// </summary>
    /// <param name="fieldSet">
    /// The field set that describes the key.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </param>
    public KeyAttribute(string? fieldSet = default)
    {
        FieldSet = fieldSet;
    }

    /// <summary>
    /// Gets the field set that describes the key.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </summary>
    public string? FieldSet { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IObjectTypeDescriptor objectTypeDescriptor when element is Type runtimeType:
            {
                if (string.IsNullOrEmpty(FieldSet))
                {
                    throw Key_FieldSet_CannotBeEmpty(runtimeType);
                }

                objectTypeDescriptor.Key(FieldSet!);
                break;
            }

            case IObjectFieldDescriptor objectFieldDescriptor when element is MemberInfo:
                objectFieldDescriptor
                    .Extend()
                    .OnBeforeCreate(d => d.ContextData[Constants.WellKnownContextData.KeyMarker] = true);
                break;
        }
    }
}
