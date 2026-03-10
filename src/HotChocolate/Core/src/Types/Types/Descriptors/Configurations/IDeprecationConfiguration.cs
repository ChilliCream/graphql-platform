#nullable disable

namespace HotChocolate.Types.Descriptors.Configurations;

public interface IDeprecationConfiguration
{
    string DeprecationReason { get; }

    bool IsDeprecated { get; }
}
