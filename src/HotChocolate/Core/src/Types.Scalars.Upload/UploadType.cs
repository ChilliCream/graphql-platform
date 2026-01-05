using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

/// <summary>
/// The GraphQL Upload scalar.
/// </summary>
public sealed class UploadType : ScalarType<IFile, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UploadType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UploadType() : base("Upload", BindingBehavior.Implicit)
    {
        Description = UploadResources.UploadType_Description;
    }

    public override object CoerceInputLiteral(StringValueNode valueLiteral)
        => throw new NotSupportedException();

    public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (inputValue.ValueKind is not JsonValueKind.String)
        {
            throw new LeafCoercionException("A file reference must be a string.", this);
        }

        var fileLookup = context.Features.Get<IFileLookup>();
        var fileName = inputValue.GetString()!;

        if (fileLookup is null || !fileLookup.TryGetFile(fileName, out var file))
        {
            throw new LeafCoercionException(
                string.Format(
                    "The specified file `{0}` could not be found.",
                    fileName),
                this);
        }

        return file;
    }

    public override void CoerceOutputValue(IFile runtimeValue, ResultElement resultValue)
        => throw new NotSupportedException();

    public override IValueNode ValueToLiteral(IFile runtimeValue)
        => throw new NotSupportedException();
}
