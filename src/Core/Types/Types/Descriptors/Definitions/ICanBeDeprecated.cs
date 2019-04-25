namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface ICanBeDeprecated
    {
        string DeprecationReason { get; }

        bool IsDeprecated { get; }
    }
}
