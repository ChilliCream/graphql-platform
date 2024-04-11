using HotChocolate.Language;
using HotChocolate.Types.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

/// <summary>
/// The GraphQL Upload scalar.
/// </summary>
public class UploadType : ScalarType<IFile, FileValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UploadType"/> class.
    /// </summary>
    public UploadType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UploadType()
        : this(
            "Upload",
            UploadResources.UploadType_Description,
            BindingBehavior.Implicit)
    {
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is IFile file)
        {
            return new FileValueNode(file);
        }

        throw base.CreateParseValueError(resultValue);
    }

    protected override IFile ParseLiteral(FileValueNode valueSyntax) =>
        valueSyntax.Value;

    protected override FileValueNode ParseValue(IFile runtimeValue) =>
        new(runtimeValue);

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        throw new GraphQLException(
            UploadResources.UploadType_TrySerialize_NotSupported);
    }

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is IFile file)
        {
            runtimeValue = file;
            return true;
        }

        runtimeValue = null;
        return false;
    }
}
