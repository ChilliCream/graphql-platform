using HotChocolate.Features;

namespace HotChocolate.Skimmed;

public sealed class TypeMetadata : ISealable
{
    private bool _isExtension;
    private bool _isReadOnly;

    public bool IsExtension
    {
        get => _isExtension;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The metadata is sealed and cannot be modified.");
            }

            _isExtension = value;
        }
    }

    public bool IsReadOnly => _isReadOnly;

    public void Seal()
    {
        if (_isReadOnly)
        {
            throw new NotSupportedException(
                "The metadata is sealed and cannot be modified.");
        }

        _isReadOnly = true;
    }
}
