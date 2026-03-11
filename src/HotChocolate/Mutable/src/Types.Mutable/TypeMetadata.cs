using HotChocolate.Features;

namespace HotChocolate.Types.Mutable;

public sealed class TypeMetadata : ISealable
{
    private bool _isReadOnly;

    public bool IsExtension
    {
        get;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The metadata is sealed and cannot be modified.");
            }

            field = value;
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
