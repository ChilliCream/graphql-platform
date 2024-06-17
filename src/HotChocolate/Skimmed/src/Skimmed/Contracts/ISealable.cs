namespace HotChocolate.Skimmed;

internal interface ISealable
{
    bool IsReadOnly { get; }

    void Seal();
}
