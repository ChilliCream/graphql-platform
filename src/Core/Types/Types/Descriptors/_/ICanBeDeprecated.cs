namespace HotChocolate.Types.Descriptors
{
    public interface ICanBeDeprecated
    {
        string DeprecationReason { get; }

        bool IsDeprecated { get; }
    }
}
