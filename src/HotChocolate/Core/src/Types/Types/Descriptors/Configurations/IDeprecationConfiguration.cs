namespace HotChocolate.Types.Descriptors.Definitions;

public interface IDeprecationConfiguration
{
    string DeprecationReason { get; }

    bool IsDeprecated { get; }
}
