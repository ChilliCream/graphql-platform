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
public sealed class UploadType : ScalarType<IFile>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UploadType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UploadType() : base("Upload", BindingBehavior.Implicit)
    {
        Description = UploadResources.UploadType_Description;
    }

    public override ScalarSerializationType SerializationType => ScalarSerializationType.String;

    /// <inheritdoc />
    public override object CoerceInputLiteral(IValueNode valueLiteral)
    {
        if (valueLiteral is not UploadValueNode uploadValue)
        {
            throw new LeafCoercionException(
                $"Cannot coerce the literal of type `{valueLiteral.Kind}` to a file.",
                this);
        }

        return uploadValue.File;
    }

    /// <summary>
    /// Coerces a JSON string value containing a file reference into an <see cref="IFile"/> instance.
    /// The file reference is looked up using the <see cref="IFileLookup"/> service from the context.
    /// </summary>
    /// <param name="inputValue">
    /// The JSON element containing the file reference as a string.
    /// </param>
    /// <param name="context">
    /// The feature provider context containing the <see cref="IFileLookup"/> service.
    /// </param>
    /// <returns>
    /// An <see cref="IFile"/> instance representing the uploaded file.
    /// </returns>
    /// <exception cref="LeafCoercionException">
    /// Thrown when the file reference cannot be found in the file lookup service.
    /// </exception>
    public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (inputValue.ValueKind is not JsonValueKind.String)
        {
            throw new LeafCoercionException(
                $"Cannot coerce the json value of kind `{inputValue.ValueKind}` to a file.",
                this);
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

    /// <summary>
    /// This operation is not supported. Upload scalars are input-only and cannot be used in output.
    /// </summary>
    /// <param name="runtimeValue">The runtime value (not used).</param>
    /// <param name="resultValue">The result element (not used).</param>
    /// <exception cref="NotSupportedException">Always thrown as output coercion is not supported.</exception>
    public override void OnCoerceOutputValue(IFile runtimeValue, ResultElement resultValue)
        => throw new NotSupportedException();

    /// <summary>
    /// This operation is not supported. Upload scalars cannot be converted to GraphQL literals.
    /// </summary>
    /// <param name="runtimeValue">The runtime value (not used).</param>
    /// <returns>Never returns; always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown as value to literal conversion is not supported.</exception>
    public override IValueNode OnValueToLiteral(IFile runtimeValue)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public override IValueNode InputValueToLiteral(JsonElement inputValue, IFeatureProvider context)
    {
        var file = (IFile)CoerceInputValue(inputValue, context);
        return new UploadValueNode(inputValue.GetString()!, file);
    }
}
